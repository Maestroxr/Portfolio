using System.Collections.Generic;
using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// The interface of the game, built from code the first time it is needed. Before a scenario it is the title screen
    /// with the campaign behind it; during one it is the adventure screen around the map (<see cref="AdventureHud"/>),
    /// and over the map the screens of a town, of a hero and of a battle, and the questions the rules ask. It also reads
    /// the pointer on the map: what it is over, the trail a hero would take, and the order a click gives. The shared
    /// menu of the framework, restyled by the scene builder, is the pause menu and the settings panel.
    /// </summary>
    public class HeroesUI : GameUI
    {
        /// <summary>The player's avatar on this device while the menu is up: In the bottom right corner, over the landscape.</summary>
        protected override (Vector2 anchor, Vector2 offset)? AvatarPlace => (new Vector2(1f, 0f), new Vector2(-24f, 24f));

        /// <summary>The key the manager keeps the level of the last saved scenario under, which Continue takes up.</summary>

        /// <summary>The pause menu's way back to the title screen, added to the shared menu by the scene builder.</summary>
        [SerializeField] private Button titleButton;

        private HeroesGameManager manager;
        private Canvas canvas;
        private RectTransform screen;
        private RectTransform safe;
        /// <summary>The holder of the screens of the game (a town, a hero, a meeting, the questions, the results), pulled in under the remote frame.</summary>
        private RectTransform windows;
        /// <summary>The holder of the battle bar, pulled in under the remote frame too.</summary>
        private RectTransform battleHolder;
        private AdventureHud hud;
        private TitleScreen title;
        private CampaignScreen campaignScreen;
        private TownScreen town;
        private HeroScreen heroSheet;
        private BattleBar battleBar;
        private ChoiceBox choice;
        private ResultsBox results;
        private MessageBox message;
        private SplitBox split;
        private DwellingBox dwelling;
        private MeetingScreen meeting;

        private bool busy;
        private int hoverCell = -1;
        private MovePlan plan;
        /// <summary>The town a click on the map sent the hero in hand into (its id), or -1: its screen opens as he arrives.</summary>
        private int enteringTown = -1;
        private CursorKind cursor = CursorKind.Default;
        private bool cursorSet;
        private BaseGameState shownState = BaseGameState.Initialization;
        private bool leaveArmed;
        private string leaveWords;
        private bool remoteView;

        public bool Busy => busy;

        /// <summary>Whether this device plays as a remote control of the room's shared screen (<see cref="SetRemoteView"/>).</summary>
        public bool RemoteView => remoteView;

        public TownScreen Town => town;

        public HeroScreen Sheet => heroSheet;

        public BattleBar Bar => battleBar;

        public AdventureHud Hud => hud;

        public TitleScreen Title => title;

        public CampaignScreen Campaign => campaignScreen;

        public ChoiceBox Choice => choice;

        public ResultsBox Results => results;

        public MessageBox Message => message;

        public SplitBox Split => split;

        public DwellingBox Dwelling => dwelling;

        public MeetingScreen Meeting => meeting;

        /// <summary>
        /// Whether the player at this device may give an order now: the rules wait for them and the map has played out
        /// everything that happened. The interface never sends a command otherwise.
        /// </summary>
        public bool CanCommand => manager != null && manager.Game != null && !busy && manager.WaitingForHuman;

        private HeroesGame Game => manager != null ? manager.Game : null;

        private IHeroesCommands Commands => manager != null ? manager.Commands : null;

        // ------------------------------------------------------------------ building

        /// <summary>
        /// Builds the whole interface once, the first time anything needs it: the title, the screen of a scenario and
        /// the screens that come before one. Everything in it is filled from the game as it goes, so it is never rebuilt.
        /// </summary>
        public void Prepare()
        {
            if (canvas != null)
            {
                return;
            }
            manager = Manager as HeroesGameManager;
            if (manager == null)
            {
                return;
            }
            UIKit.Art = manager.Art;
            Clicker.Audio = manager.Sound;
            if (manager.Sound != null)
            {
                // The sound follows the settings in use, the player's own once they are switched on.
                manager.Sound.Source = () => manager.Options;
            }
            canvas = MakeCanvas();
            screen = (RectTransform)canvas.transform;
            safe = MakeSafeArea(screen);

            hud = AdventureHud.Make(safe, manager, this);
            // The battle bar and the screens of the game sit in holders of their own: as a remote control of a shared
            // screen they are pulled in between the header and the footer of the remote frame (SetRemoteView).
            battleHolder = UIKit.Stretch(UIKit.Rect(safe, "BattleHolder"));
            windows = UIKit.Stretch(UIKit.Rect(safe, "Windows"));
            town = TownScreen.Make(windows, manager);
            heroSheet = HeroScreen.Make(windows, manager);
            battleBar = BattleBar.Make(battleHolder, manager);
            choice = ChoiceBox.Make(windows, manager);
            results = ResultsBox.Make(windows, manager);
            campaignScreen = CampaignScreen.Make(windows, manager);
            message = MessageBox.Make(windows, manager);
            dwelling = DwellingBox.Make(windows, manager);
            meeting = MeetingScreen.Make(windows, manager);
            split = SplitBox.Make(windows, manager);
            title = TitleScreen.Make(screen, safe, manager, this);
            Tooltip.Prepare(screen);
            CloseAll();
            ShowHud(false);
        }

        /// <summary>
        /// Throws the whole interface away and builds it again from the art of the active theme, the title (and the
        /// valley behind it) showing again if it showed before. For the title screen and the screens before a scenario
        /// only: a scenario in progress keeps its interface.
        /// </summary>
        public void Rebuild()
        {
            if (canvas == null)
            {
                return;
            }
            bool titleShown = title != null && title.IsShown;
            title?.Hide();
            TooltipBox.Hide(this);
            Destroy(canvas.gameObject);
            canvas = null;
            Prepare();
            if (titleShown && title != null)
            {
                title.Show();
            }
            cursorSet = false;
            SetCursor(CursorKind.Default);
        }

        /// <summary>A scenario is starting: the panels are cleared and pointed at it.</summary>
        public void Bind(HeroesGameManager owner)
        {
            manager = owner;
            Prepare();
            hud.ClearLog();
            chronicles.Clear();
            chronicleOf = -1;
            CloseAll();
            title.Hide();
            ShowHud(true);
            if (manager.IsOnlineGame)
            {
                Log(manager.PlaysSavedGame ? "A saved game of the room goes on." : "A game of the room. Take the land.", -1);
            }
            else
            {
                Log(manager.Scenario != null ? manager.Scenario.Goal : "Take the land.", -1);
            }
        }

        /// <summary>
        /// Shows or hides the panels of the adventure map (the resources, the heroes and towns, the commands, the log and
        /// the little map): a battle hides them while it is fought, in either style.
        /// </summary>
        public void ShowAdventureHud(bool on)
        {
            ShowHud(on);
        }

        /// <summary>The panels of a scenario are hidden while there is none.</summary>
        public void ShowHud(bool on)
        {
            hud?.Show(on);
            if (!on)
            {
                TooltipBox.Hide(this);
            }
        }

        /// <summary>
        /// This device plays as a remote control of the room's shared screen (<paramref name="remote"/>), or not any more.
        /// As a remote the map stays the control under the panel's frame, which has the treasury, the hero in hand, the
        /// questions and the commands of the seat (<see cref="RemotePanel"/>): the bars and the column of the adventure
        /// map and the choice dialog keep out of its way, and come back on the player's own device.
        /// </summary>
        public void SetRemoteView(bool remote)
        {
            remoteView = remote;
            if (hud != null)
            {
                hud.Compact = remote;
            }
            // The frame's header and footer cover the top and the bottom of the screen: the battle bar and the screens of
            // the game are pulled in between them (the screens scaled down so the tall ones fit), and have the whole
            // height again on the player's own device.
            float top = remote ? Gamebox.Online.RemoteControlUI.HeaderHeight : 0f;
            float bottom = remote ? Gamebox.Online.RemoteControlUI.FooterHeight : 0f;
            if (battleHolder != null)
            {
                UIKit.Stretch(battleHolder, 0f, bottom, 0f, top);
            }
            if (windows != null)
            {
                UIKit.Stretch(windows, 0f, bottom, 0f, top);
                windows.localScale = Vector3.one * (remote ? WindowsFit : 1f);
            }
            if (remote)
            {
                choice?.Close();
                TooltipBox.Hide(this);
            }
        }

        /// <summary>
        /// How much the screens of the game are scaled down under the remote frame: the band between its header and its
        /// footer over the height the tallest screens (a town, a hero) were laid out for.
        /// </summary>
        private const float WindowsFit = (1080f - Gamebox.Online.RemoteControlUI.HeaderHeight - Gamebox.Online.RemoteControlUI.FooterHeight) / 1000f;

        /// <summary>
        /// Whether a window of the game lies over the map: a town, a hero, a meeting, a dwelling, a split, a message, the
        /// campaign, the results of the game or of a battle. Under the remote frame the panel then offers nothing.
        /// </summary>
        public bool WindowOpen => ScreenOpen || battleBar != null && battleBar.Results.IsOpen;

        /// <summary>How much of the bottom of the screen the footer of the remote frame takes from a battle, in canvas units (0 on the player's own device).</summary>
        public float BattleBottomInset => remoteView ? Gamebox.Online.RemoteControlUI.FooterHeight : 0f;

        /// <summary>How much of the top of the screen the header of the remote frame and the panel's battle commands take from a battle, in canvas units.</summary>
        public float BattleTopInset => remoteView ? Gamebox.Online.RemoteControlUI.HeaderHeight + RemotePanel.BattleRowHeight : 0f;

        private Canvas MakeCanvas()
        {
            var go = new GameObject("HeroesCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var made = go.GetComponent<Canvas>();
            made.renderMode = RenderMode.ScreenSpaceOverlay;
            made.sortingOrder = 10;
            // As every Gamebox canvas: the 1920x1080 layout grows to fill any screen rather than being cut by it.
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            if (EventSystem.current == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
            return made;
        }

        /// <summary>The part of the screen no notch or rounded corner covers, where everything that can be read goes.</summary>
        private static RectTransform MakeSafeArea(RectTransform parent)
        {
            RectTransform area = UIKit.Stretch(UIKit.Rect(parent, "SafeArea"));
            var safeArea = area.gameObject.AddComponent<SafeArea>();
            safeArea.ReferenceOrientation = ScreenOrientation.LandscapeLeft;
            safeArea.Edges = SafeArea.SafeAreaMode.Left | SafeArea.SafeAreaMode.Right | SafeArea.SafeAreaMode.Top | SafeArea.SafeAreaMode.Bottom;
            safeArea.Alignment = SafeArea.AlignmentMode.CenterHorizontally;
            return area;
        }

        protected override void Awake()
        {
            base.Awake();
            Prepare();
            if (titleButton != null)
            {
                titleButton.onClick.AddListener(LeaveToTitle);
            }
        }

        /// <summary>
        /// The pause menu's Leave to the Title. A scenario is saved on its way out, but no game is saved in the middle of
        /// a battle: there the first press says what would be lost and the button asks again, and only a second press
        /// leaves.
        /// </summary>
        private void LeaveToTitle()
        {
            if (manager == null)
            {
                return;
            }
            HeroesGame game = manager.Game;
            bool loses = game != null && game.InBattle && !game.IsOver && !manager.InSession;
            if (loses && !leaveArmed)
            {
                ArmLeave(true);
                UpdateError("A battle cannot be saved: leaving now loses it, and all that was done since the game was last saved.");
                return;
            }
            ArmLeave(false);
            manager.ReturnToTitle();
        }

        /// <summary>Turns the pause menu's Leave to the Title into its second, final press, or back.</summary>
        private void ArmLeave(bool on)
        {
            leaveArmed = on;
            TMP_Text label = titleButton != null ? titleButton.GetComponentInChildren<TMP_Text>(true) : null;
            if (label == null)
            {
                return;
            }
            leaveWords ??= label.text;
            label.text = on ? Words.T("Leave and Lose the Battle") : leaveWords;
        }

        // ------------------------------------------------------------------ the title, the menus and the way back

        /// <summary>
        /// The title screen stands in for the shared menu before a game and after it; the shared menu is the pause menu.
        /// A game's end leaves its results box on screen instead of the menu.
        /// </summary>
        public override void UpdateGameState(Gamebox.GameState state)
        {
            shownState = state.BaseState;
            Prepare();
            ArmLeave(false);
            base.UpdateGameState(state);
            switch (state.BaseState)
            {
                case BaseGameState.Initialization:
                    HideMenu();
                    ShowHud(false);
                    town?.Close();
                    heroSheet?.Close();
                    choice?.Close();
                    battleBar?.Hide();
                    hud?.HideAnnouncement();
                    SetCursor(CursorKind.Default);
                    title?.Show();
                    break;
                case BaseGameState.Running:
                    title?.Hide();
                    break;
                case BaseGameState.Paused:
                    TooltipBox.Hide(this);
                    break;
                default:
                    HideMenu();
                    break;
            }
        }

        /// <summary>The settings panel goes back to where it was opened from: the title, or the pause menu.</summary>
        public override void HideSettings()
        {
            base.HideSettings();
            if (shownState == BaseGameState.Initialization)
            {
                HideMenu();
                title?.Show();
            }
        }

        public override void EnableLoad()
        {
            base.EnableLoad();
            title?.Refresh();
        }

        /// <summary>Escape closes what lies over the map first: a message, the market, a town, a hero, the campaign.</summary>
        public override bool HandleBack()
        {
            if (base.HandleBack())
            {
                return true;
            }
            if (message != null && message.IsOpen)
            {
                message.Close();
                return true;
            }
            if (split != null && split.IsOpen)
            {
                split.Close();
                return true;
            }
            if (dwelling != null && dwelling.IsOpen)
            {
                dwelling.Close();
                return true;
            }
            if (meeting != null && meeting.IsOpen)
            {
                meeting.Close();
                return true;
            }
            if (town != null && town.HandleBack())
            {
                return true;
            }
            if (heroSheet != null && heroSheet.IsOpen)
            {
                heroSheet.Close();
                return true;
            }
            if (results != null && results.IsOpen)
            {
                // The end of a game: Escape does what its one button does.
                results.Leave();
                return true;
            }
            if (campaignScreen != null && campaignScreen.IsOpen)
            {
                campaignScreen.Close();
                return true;
            }
            return false;
        }

        /// <summary>The shared menu's New Game opens the campaign instead of starting one straight away.</summary>
        protected override void OnStartNewGameClicked()
        {
            if (!OpenCampaign())
            {
                base.OnStartNewGameClicked();
            }
        }

        /// <summary>Shows the chapters, or the skirmish maps. False when the interface is not up yet.</summary>
        public bool OpenCampaign(bool skirmish = false)
        {
            Prepare();
            if (campaignScreen == null)
            {
                return false;
            }
            HideMenu();
            campaignScreen.Show(skirmish);
            return true;
        }

        /// <summary>The campaign was closed with Back: the title shows again (the title is where it was opened from).</summary>
        public void CampaignClosed()
        {
            if (shownState == BaseGameState.Initialization && !manager.InSession)
            {
                title.Show();
            }
        }

        /// <summary>
        /// Where the battles of new games are fought, chosen on the campaign screen: it becomes one of the player's own
        /// settings, saved like the settings panel saves them. The defaults are never changed, so while they are in use
        /// the player's saved settings come back first, as "Use My Settings" brings them, and only the style changes.
        /// </summary>
        public void SetBattleStyle(int style)
        {
            HeroesSettings active = manager.Options;
            if (active == null || active.battleStyle == style)
            {
                return;
            }
            if (manager.UsingDefaultSettings)
            {
                manager.LoadCustomSettings();
            }
            if (manager.CustomSettings is not HeroesSettings custom)
            {
                return;
            }
            if (GameSettingsUI is HeroesSettingsUI panel)
            {
                // The panel is what is saved: it shows the player's settings as they stand, with the new style.
                panel.UpdateFromSettings(custom);
                panel.SetBattleStyle(style);
                manager.SaveCustomSettings();
                panel.UsingDefault(false);
            }
            else
            {
                custom.battleStyle = style;
                manager.SaveCustomSettings();
            }
        }

        /// <summary>
        /// The Menu command of the map: the pause menu at one device (the game stops under it), the match menu of the
        /// lobby online (it does not).
        /// </summary>
        public void OpenMenu()
        {
            TooltipBox.Hide(this);
            OnPauseClicked();
        }

        /// <summary>After an online game, back to its room in the lobby.</summary>
        public void LeaveToRoom()
        {
            IGameController controller = ActiveController;
            if (controller != null)
            {
                controller.TransitionState(BaseGameState.Initialization);
            }
            else
            {
                manager.ReturnToTitle();
            }
        }

        /// <summary>The credits of the art, the music and the fonts, from the credits file of the art.</summary>
        public void OpenCredits()
        {
            Prepare();
            TextAsset credits = manager.Art != null ? manager.Art.credits : null;
            message.ShowCredits(credits != null ? credits.text : Words.T("Heroes"));
        }

        public void EndTurn()
        {
            if (CanCommand)
            {
                Commands?.EndTurn();
            }
        }

        // ------------------------------------------------------------------ what the manager asks for

        public void CloseAll()
        {
            campaignScreen?.Close();
            town?.Close();
            heroSheet?.Close();
            choice?.Close();
            results?.Close();
            message?.Close();
            split?.Close();
            dwelling?.Close();
            meeting?.Close();
            battleBar?.Hide();
            hud?.HideAnnouncement();
        }

        public void SetBusy(bool on)
        {
            busy = on;
            if (on)
            {
                manager.Path?.Clear();
                hoverCell = -1;
                TooltipBox.Hide(this);
            }
            Refresh();
        }

        /// <summary>
        /// The language changed: before a scenario the interface is built again in it (as for a new theme); during one,
        /// everything code wrote is written again (the static words follow by themselves, see TranslatedLabel).
        /// </summary>
        public override void RefreshTexts()
        {
            base.RefreshTexts();
            leaveWords = null;
            if (canvas == null)
            {
                return;
            }
            if (Game == null)
            {
                Rebuild();
                return;
            }
            Refresh();
            battleBar?.RefreshTexts();
            choice?.RefreshTexts();
            results?.RefreshTexts();
        }

        /// <summary>Redraws everything that shows a number: resources, the date, the heroes, the little map.</summary>
        public void Refresh()
        {
            if (Game == null || canvas == null)
            {
                return;
            }
            hud.Refresh(busy);
            town?.Refresh();
            heroSheet?.Refresh();
            meeting?.Refresh();
            dwelling?.Refresh();
            split?.Refresh();
        }

        public void Log(string text, int player)
        {
            if (string.IsNullOrEmpty(text) || hud == null)
            {
                return;
            }
            if (manager != null && manager.SharesDevice)
            {
                Chronicle(chronicleOf).Add((text, player));
            }
            Write(text, player);
        }

        private void Write(string text, int player)
        {
            // On the parchment a player's lines are written in a darker ink of their colour.
            int color = PlayerColorOf(player);
            hud.Log(text, player >= 0 && color >= 0 ? LogInk(HeroesArt.PlayerColor(color)) : UIKit.InkOnParchment);
        }

        // ------------------------------------------------------------------ a chronicle for every person of a hot seat

        /// <summary>The most lines a person's chronicle keeps (the log shows fewer).</summary>
        private const int ChronicleLines = 60;

        /// <summary>
        /// The lines of the log of each person of a hot seat (or of an online game several people play here), by seat; -1
        /// holds what was written before anybody had the device (the goal of the map), which every chronicle begins with.
        /// </summary>
        private readonly Dictionary<int, List<(string text, int player)>> chronicles =
            new Dictionary<int, List<(string text, int player)>>();

        private int chronicleOf = -1;

        private List<(string text, int player)> Chronicle(int seat)
        {
            if (!chronicles.TryGetValue(seat, out var lines))
            {
                lines = seat >= 0 && chronicles.TryGetValue(-1, out var common)
                    ? new List<(string text, int player)>(common)
                    : new List<(string text, int player)>();
                chronicles[seat] = lines;
            }
            if (lines.Count > ChronicleLines)
            {
                lines.RemoveRange(0, lines.Count - ChronicleLines);
            }
            return lines;
        }

        /// <summary>
        /// The device went to another person of a hot seat: the log shows their own chronicle, what they found and what
        /// they saw happen, and none of what the others did.
        /// </summary>
        public void SwitchChronicle(int seat)
        {
            if (hud == null || seat == chronicleOf)
            {
                return;
            }
            chronicleOf = seat;
            hud.ClearLog();
            foreach ((string text, int player) in Chronicle(seat))
            {
                Write(text, player);
            }
        }

        private int PlayerColorOf(int player)
        {
            PlayerState state = Game?.State.Player(player);
            return state != null ? (int)state.color : -1;
        }

        /// <summary>The parchment of the log as the eye sees it behind the lines (its texture's middle tone).</summary>
        private static readonly Color LogPaper = new Color(0.9f, 0.816f, 0.631f);

        /// <summary>
        /// A realm's colour as ink on the log's parchment: mixed toward the parchment's own brown ink, at least as much as
        /// suits the deep red and blue, and further for the light green and yellow, until it reads at 5 to 1 or better.
        /// </summary>
        private static Color LogInk(Color color)
        {
            for (float mix = 0.42f; mix < 1f; mix += 0.04f)
            {
                Color ink = Color.Lerp(color, UIKit.InkOnParchment, mix);
                if (Contrast(ink, LogPaper) >= 5f)
                {
                    return ink;
                }
            }
            return UIKit.InkOnParchment;
        }

        /// <summary>The contrast of two colours as the web's guidelines measure it, from 1 (the same) to 21.</summary>
        private static float Contrast(Color a, Color b)
        {
            float first = Luminance(a);
            float second = Luminance(b);
            return (Mathf.Max(first, second) + 0.05f) / (Mathf.Min(first, second) + 0.05f);
        }

        private static float Luminance(Color color)
        {
            Color linear = color.linear;
            return 0.2126f * linear.r + 0.7152f * linear.g + 0.0722f * linear.b;
        }

        /// <summary>The banner that names the day, the week or the town that just changed hands.</summary>
        public void Announce(string heading, string body)
        {
            hud?.Announce(Words.T(heading), TurnLine(body));
        }

        /// <summary>
        /// A new day is announced with the name of the realm whose turn it is: the banner says it as a turn, "Your turn"
        /// for the player at this device, with the days a scenario of a deadline has left. Any other line is shown as it is.
        /// </summary>
        private string TurnLine(string body)
        {
            GameState state = Game?.State;
            if (state == null || string.IsNullOrEmpty(body))
            {
                return Words.Sentence(body);
            }
            foreach (PlayerState player in state.players)
            {
                if (player.name == body)
                {
                    if (player.index != manager.Viewer || manager.SharesDevice)
                    {
                        // In a hot seat (and online with several people here) every person is called by name.
                        return Words.F("{0}'s turn", Words.Name(player.name));
                    }
                    string left = DaysLeft(false);
                    return left.Length > 0 ? $"{Words.T("Your turn")}  -  {left}" : Words.T("Your turn");
                }
            }
            return Words.Sentence(body);
        }

        /// <summary>
        /// What the scenario asks of the player at this device, for the tooltip of the date: the task, what loses it
        /// besides losing everything, and the days left when it has a deadline.
        /// </summary>
        public string TaskText()
        {
            HeroesLevel scenario = manager != null && !manager.IsOnlineGame ? manager.Scenario : null;
            string text = scenario != null ? Words.T(scenario.Goal) : Words.T("Take the land.");
            if (scenario != null && !string.IsNullOrEmpty(scenario.Loss))
            {
                text += "\n" + $"{Words.T("Beware")}: {Words.T(scenario.Loss)}";
            }
            string left = DaysLeft(true);
            return left.Length > 0 ? text + "\n" + left : text;
        }

        /// <summary>
        /// The days left to a scenario's deadline, as a line: the days to hold out until a day that wins it, or (always,
        /// or in its last two weeks) the days left to win before a day limit; empty without a deadline.
        /// </summary>
        private string DaysLeft(bool always)
        {
            GameState state = Game?.State;
            if (state == null || manager.IsOnlineGame)
            {
                return "";
            }
            ScenarioRules rules = state.rules;
            if (rules.victory == VictoryKind.Survive && rules.victoryValue > state.day)
            {
                int days = rules.victoryValue - state.day;
                return days == 1 ? Words.T("One more day to hold out.") : Words.F("{0} more days to hold out.", days);
            }
            if (rules.loss == LossKind.TimeLimit && rules.dayLimit > 0)
            {
                int days = rules.dayLimit - state.day + 1;
                if (days > 0 && (always || days <= 14))
                {
                    return days == 1 ? Words.T("The last day to win.") : Words.F("{0} days left to win.", days);
                }
            }
            return "";
        }

        public void ShowTurn(int who)
        {
            Refresh();
        }

        public void ShowChoice(PendingChoice pending)
        {
            if (remoteView)
            {
                // The remote panel asks the question; the dialog would stand over the map under the panel's frame.
                choice.Close();
                return;
            }
            choice.Show(pending);
        }

        public void ShowBattleTurn(BattleState battle)
        {
            battleBar.Show(battle);
        }

        public void BeginBattle(BattleState battle)
        {
            CloseAll();
            hud.CloseLog();
            TooltipBox.Hide(this);
            battleBar.Begin(battle);
        }

        public void EndBattle(BattleResult result, BattleState battle)
        {
            battleBar.Finish(result, battle);
        }

        public void SetBattleRound(int round)
        {
            battleBar.SetRound(round);
        }

        public void ShowEnd(bool won, int stars, int days)
        {
            CloseAll();
            results.Show(won, stars, days);
        }

        /// <summary>The end of a hot seat (or of an online game several people played here): who won, and how every realm stood.</summary>
        public void ShowHotSeatEnd(GameState state, int days)
        {
            CloseAll();
            results.ShowHotSeat(state, days);
        }

        /// <summary>
        /// Closes the screens a person may have open over the map (a town, a hero, a meeting, a dwelling, a message), so
        /// the next person of a hot seat finds none of them.
        /// </summary>
        public void CloseScreens()
        {
            town?.Close();
            heroSheet?.Close();
            meeting?.Close();
            dwelling?.Close();
            split?.Close();
            message?.Close();
            choice?.Close();
            hud?.HideAnnouncement();
            TooltipBox.Hide(this);
            manager?.Path?.Clear();
            hoverCell = -1;
        }

        public void OpenHero()
        {
            if (manager.Selected != null)
            {
                heroSheet.Open(manager.Selected);
            }
        }

        public void OpenTown()
        {
            // (On the shared screen of a room: the first town of the realm whose turn it is, to look at.)
            PlayerState viewer = manager.WatchedState;
            if (viewer != null && viewer.towns.Count > 0)
            {
                town.Open(Game.State.Town(viewer.towns[0]));
            }
        }

        /// <summary>Whether a screen of the game lies over the map (the map then takes no clicks).</summary>
        private bool ScreenOpen => town.IsOpen || heroSheet.IsOpen || choice.IsOpen || results.IsOpen || message.IsOpen ||
                                   campaignScreen.IsOpen || meeting.IsOpen || dwelling.IsOpen || split.IsOpen;

        // ------------------------------------------------------------------ the pointer on the map

        private void Update()
        {
            if (manager == null)
            {
                return;
            }
            if (manager.Rig != null && manager.Options != null)
            {
                manager.Rig.EdgeScroll = manager.Options.edgeScroll;
            }
            // Online, whose turn it is and the clock are on the bar along the top of the map (over a town or a hero too)
            // and in a battle on the battle bar, by the round: the turn banner of the lobby, which would stand over the
            // field and the battle's opening words, stays off.
            Gamebox.Online.OnlineLobbyUI lobby = manager.Online != null ? manager.Online.LobbyUI : null;
            if (lobby != null)
            {
                lobby.ShowTurnBanner = false;
            }
            if (Game == null || !manager.IsGameRunning)
            {
                // Over the pause menu, the settings or the end of a game the pointer is the plain one, whatever the map
                // or the field showed under it.
                MapTip(-1);
                SetCursor(CursorKind.Default);
                battleBar?.ForgetPointer();
                return;
            }
            if (Game.InBattle || manager.Battle.Running)
            {
                // The battle bar reads the pointer on the field of the battle, whichever style it is fought in.
                MapTip(-1);
                cursorSet = false;
                battleBar?.Pointer();
                return;
            }
            AdventureHover();
        }

        /// <summary>
        /// Development tours only: a place on the screen that stands in for the pointer on the map, so a tour can show
        /// what hovering over something looks like. Null for the real pointer.
        /// </summary>
        public Vector3? TourPointer { get; set; }

        /// <summary>Development tours only: a click at <see cref="TourPointer"/>, taken on the next frame the map is read.</summary>
        public bool TourClick { get; set; }

        /// <summary>The cell the pointer is over, or -1 when it is over the interface or off the map.</summary>
        private int PointerCell()
        {
            if (manager.Rig == null || manager.Map == null)
            {
                return -1;
            }
            if (TourPointer == null && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return -1;
            }
            return manager.Rig.PointerGround(TourPointer ?? Input.mousePosition, out Vector3 point, manager.Map.GroundMask)
                ? manager.Map.CellAt(point)
                : -1;
        }

        private void AdventureHover()
        {
            // Nothing on the map answers the pointer behind the hand-over screen of a hot seat, nor the click that lifts it.
            int cell = busy || ScreenOpen || manager.IsPassing ? -1 : PointerCell();
            HeroState hero = manager.Selected;
            if (cell != hoverCell)
            {
                hoverCell = cell;
                plan = hero != null && cell >= 0 ? manager.PlanFor(hero, cell) : null;
                if (plan != null && plan.Cells.Count > 0)
                {
                    manager.Path.Show(hero.cell, plan.Cells, plan.Today);
                }
                else
                {
                    manager.Path?.Clear();
                }
                MapTip(cell);
            }
            SetCursor(cell < 0 ? CursorKind.Default : CursorFor(cell, hero));
            bool click = TourClick || Input.GetMouseButtonDown(0);
            TourClick = false;
            if (cell >= 0 && click && !manager.Rig.IsDragging)
            {
                Click(cell);
            }
        }

        /// <summary>
        /// A click on the map: pick up a hero of your own, send the hero in hand into a town of your own he gets to
        /// today (its screen opens as he arrives) or open the town when he does not, else send the hero there.
        /// </summary>
        private void Click(int cell)
        {
            GameState state = Game.State;
            enteringTown = -1;
            HeroState standing = state.HeroAt(cell);
            HeroState selected = manager.Selected;
            if (standing != null && standing.owner == manager.Viewer)
            {
                if (selected != null && selected.id == standing.id)
                {
                    heroSheet.Open(standing);
                }
                else if (selected != null && Game.Grid.Distance(selected.cell, standing.cell) <= 1)
                {
                    // Two heroes side by side trade at once, with no step to take.
                    meeting.Show(selected, standing);
                }
                else if (selected != null && Meets(selected, cell))
                {
                    // The hero in hand walks over to the other; their meeting opens as he gets there.
                    Commands?.MoveHero(selected.id, cell);
                }
                else
                {
                    manager.Select(standing);
                }
                return;
            }
            MapObject what = state.ObjectAt(cell);
            if (what != null && what.kind == ObjectKind.Dwelling && what.owner == manager.Viewer && selected != null &&
                selected.owner == manager.Viewer && Game.Grid.Distance(selected.cell, what.cell) <= 1)
            {
                // A dwelling of the realm the hero already stands beside opens without another visit (a new one is visited
                // first, which raises the realm's flag over it).
                dwelling.Show(what, selected);
                return;
            }
            if (what != null && what.kind == ObjectKind.Town)
            {
                TownState here = state.Town(what.subtype);
                if (here != null && here.owner == manager.Viewer)
                {
                    if (WalksIn(here, manager.Selected))
                    {
                        enteringTown = here.id;
                        Commands?.MoveHero(manager.Selected.id, cell);
                    }
                    else
                    {
                        town.Open(here);
                    }
                    return;
                }
            }
            if (manager.Selected != null && plan != null && plan.Cells.Count > 0 && CanCommand)
            {
                Commands?.MoveHero(manager.Selected.id, cell);
            }
        }

        /// <summary>
        /// Whether a click on <paramref name="here"/>, a town of the player's own under the pointer, sends
        /// <paramref name="hero"/> in rather than opening its screen: he can be given an order, he is not in it already,
        /// and the trail under the pointer ends in its gate today. A town out of his reach, or with another hero in its
        /// gate, opens as it does with no hero in hand; the list of towns opens any of them without moving anyone.
        /// </summary>
        private bool WalksIn(TownState here, HeroState hero)
        {
            return hero != null && hero.cell != here.cell && CanCommand && plan != null && plan.End == PathEnd.EnterTown &&
                   plan.Destination == here.cell && plan.ReachesToday;
        }

        /// <summary>
        /// A hero of the player at this device walked into <paramref name="entered"/>: when a click on the town sent
        /// him, its screen opens with him in it, his army next to the garrison.
        /// </summary>
        /// <summary>Whether the hero in hand can walk to <paramref name="cell"/> today to meet the hero of his own standing there.</summary>
        private bool Meets(HeroState hero, int cell)
        {
            MovePlan path = manager.PlanFor(hero, cell);
            // Only a meeting reached today: a click on a hero farther away picks that hero up instead.
            return path != null && path.Today > 0 && path.ReachesToday && path.End == PathEnd.Meet;
        }

        /// <summary>A hero of this device met another of the same realm: the screen where they trade opens.</summary>
        public void Met(HeroState visitor, HeroState host)
        {
            if (visitor != null && host != null)
            {
                meeting.Show(visitor, host);
            }
        }

        /// <summary>A hero of this device visited a dwelling on the map: its recruiting opens, whatever the treasury holds.</summary>
        public void VisitedDwelling(MapObject visited, HeroState hero)
        {
            dwelling.Show(visited, hero);
        }

        public void EnteredTown(TownState entered)
        {
            if (entered == null || entered.id != enteringTown)
            {
                return;
            }
            enteringTown = -1;
            town.Open(entered);
        }

        /// <summary>The pointer of the map: what a click on the cell would do.</summary>
        private CursorKind CursorFor(int cell, HeroState hero)
        {
            GameState state = Game.State;
            HeroState standing = state.HeroAt(cell);
            MapObject what = state.ObjectAt(cell);
            if (standing != null && standing.owner == manager.Viewer)
            {
                bool meets = hero != null && hero.id != standing.id && (Game.Grid.Distance(hero.cell, cell) <= 1 || Meets(hero, cell));
                return meets ? CursorKind.Visit : CursorKind.Hand;
            }
            if (what != null && what.kind == ObjectKind.Town && state.Town(what.subtype) is TownState here &&
                here.owner == manager.Viewer)
            {
                return WalksIn(here, hero) ? CursorKind.Visit : CursorKind.Hand;
            }
            if (hero == null)
            {
                return CursorKind.Default;
            }
            if (plan == null || plan.Cells.Count == 0)
            {
                return cell == hero.cell ? CursorKind.Hand : CursorKind.Blocked;
            }
            if (standing != null || what != null && what.kind == ObjectKind.Monster)
            {
                return CursorKind.Fight;
            }
            if (what != null && what.kind == ObjectKind.Town)
            {
                TownState target = state.Town(what.subtype);
                return target != null && target.owner != manager.Viewer && !target.garrison.IsEmpty ? CursorKind.Fight : CursorKind.Visit;
            }
            if (what != null)
            {
                return MapObjects.IsPickup(what.kind) ? CursorKind.Take : CursorKind.Visit;
            }
            return CursorKind.Travel;
        }

        private void SetCursor(CursorKind kind)
        {
            if (cursorSet && kind == cursor || manager == null || manager.Art == null)
            {
                return;
            }
            cursor = kind;
            cursorSet = true;
            manager.UseCursor(kind);
        }

        /// <summary>The tooltip of what stands on a cell, with its picture; nothing for bare ground.</summary>
        private void MapTip(int cell)
        {
            if (cell < 0 || Game == null)
            {
                TooltipBox.Hide(this);
                return;
            }
            if (MapTips.Describe(manager, cell, out string heading, out string body, out Sprite picture))
            {
                // A town of the player's own says what a click on it does: the hero in hand walks in, or it opens.
                MapObject what = Game.State.ObjectAt(cell);
                if (what != null && what.kind == ObjectKind.Town && Game.State.HeroAt(cell) == null &&
                    Game.State.Town(what.subtype) is TownState here && here.owner == manager.Viewer)
                {
                    body += "\n<i>" + (WalksIn(here, manager.Selected)
                        ? Words.F("Click to send {0} in.", Words.Name(manager.Selected.Name))
                        : Words.T("Click to open the town.")) + "</i>";
                }
                TooltipBox.Show(this, heading, body, picture);
            }
            else
            {
                TooltipBox.Hide(this);
            }
        }
    }
}
