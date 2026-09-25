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
        /// <summary>The key the manager keeps the level of the last saved scenario under, which Continue takes up.</summary>

        /// <summary>The pause menu's way back to the title screen, added to the shared menu by the scene builder.</summary>
        [SerializeField] private Button titleButton;

        private HeroesGameManager manager;
        private Canvas canvas;
        private RectTransform screen;
        private RectTransform safe;
        private AdventureHud hud;
        private TitleScreen title;
        private CampaignScreen campaignScreen;
        private TownScreen town;
        private HeroScreen heroSheet;
        private BattleBar battleBar;
        private ChoiceBox choice;
        private ResultsBox results;
        private MessageBox message;

        private bool busy;
        private int hoverCell = -1;
        private MovePlan plan;
        private CursorKind cursor = CursorKind.Default;
        private bool cursorSet;
        private BaseGameState shownState = BaseGameState.Initialization;
        private bool leaveArmed;
        private string leaveWords;

        public bool Busy => busy;

        public TownScreen Town => town;

        public HeroScreen Sheet => heroSheet;

        public BattleBar Bar => battleBar;

        public AdventureHud Hud => hud;

        public TitleScreen Title => title;

        public CampaignScreen Campaign => campaignScreen;

        public ChoiceBox Choice => choice;

        public ResultsBox Results => results;

        public MessageBox Message => message;

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
            town = TownScreen.Make(safe, manager);
            heroSheet = HeroScreen.Make(safe, manager);
            battleBar = BattleBar.Make(safe, manager);
            choice = ChoiceBox.Make(safe, manager);
            results = ResultsBox.Make(safe, manager);
            campaignScreen = CampaignScreen.Make(safe, manager);
            message = MessageBox.Make(safe, manager);
            title = TitleScreen.Make(screen, safe, manager, this);
            Tooltip.Prepare(screen);
            CloseAll();
            ShowHud(false);
        }

        /// <summary>A scenario is starting: the panels are cleared and pointed at it.</summary>
        public void Bind(HeroesGameManager owner)
        {
            manager = owner;
            Prepare();
            hud.ClearLog();
            CloseAll();
            title.Hide();
            ShowHud(true);
            if (manager.IsOnlineGame)
            {
                Log("A game of the room. Take the land.", -1);
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
            label.text = on ? "Leave and Lose the Battle" : leaveWords;
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
            message.ShowCredits(credits != null ? credits.text : "Heroes");
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
        }

        public void Log(string text, int player)
        {
            if (string.IsNullOrEmpty(text) || hud == null)
            {
                return;
            }
            // On the parchment a player's lines are written in a darker ink of their colour.
            int color = PlayerColorOf(player);
            hud.Log(text, player >= 0 && color >= 0 ? LogInk(HeroesArt.PlayerColor(color)) : UIKit.InkOnParchment);
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
            hud?.Announce(heading, TurnLine(body));
        }

        /// <summary>
        /// A new day is announced with the name of the realm whose turn it is: the banner says it as a turn, "Your turn"
        /// for the player at this device. Any other line is shown as it is.
        /// </summary>
        private string TurnLine(string body)
        {
            GameState state = Game?.State;
            if (state == null || string.IsNullOrEmpty(body))
            {
                return body;
            }
            foreach (PlayerState player in state.players)
            {
                if (player.name == body)
                {
                    return player.index == manager.Viewer ? "Your turn" : $"{player.name}'s turn";
                }
            }
            return body;
        }

        public void ShowTurn(int who)
        {
            Refresh();
        }

        public void ShowChoice(PendingChoice pending)
        {
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

        public void OpenHero()
        {
            if (manager.Selected != null)
            {
                heroSheet.Open(manager.Selected);
            }
        }

        public void OpenTown()
        {
            PlayerState viewer = manager.ViewerState;
            if (viewer != null && viewer.towns.Count > 0)
            {
                town.Open(Game.State.Town(viewer.towns[0]));
            }
        }

        /// <summary>Whether a screen of the game lies over the map (the map then takes no clicks).</summary>
        private bool ScreenOpen => town.IsOpen || heroSheet.IsOpen || choice.IsOpen || results.IsOpen || message.IsOpen ||
                                   campaignScreen.IsOpen;

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
            int cell = busy || ScreenOpen ? -1 : PointerCell();
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
            if (cell >= 0 && Input.GetMouseButtonDown(0) && !manager.Rig.IsDragging)
            {
                Click(cell);
            }
        }

        /// <summary>A click on the map: pick up a hero or a town of your own, else send the hero there.</summary>
        private void Click(int cell)
        {
            GameState state = Game.State;
            HeroState standing = state.HeroAt(cell);
            if (standing != null && standing.owner == manager.Viewer)
            {
                if (manager.Selected != null && manager.Selected.id == standing.id)
                {
                    heroSheet.Open(standing);
                }
                else
                {
                    manager.Select(standing);
                }
                return;
            }
            MapObject what = state.ObjectAt(cell);
            if (what != null && what.kind == ObjectKind.Town)
            {
                TownState here = state.Town(what.subtype);
                if (here != null && here.owner == manager.Viewer &&
                    (manager.Selected == null || manager.Selected.cell != cell))
                {
                    town.Open(here);
                    return;
                }
            }
            if (manager.Selected != null && plan != null && plan.Cells.Count > 0 && CanCommand)
            {
                Commands?.MoveHero(manager.Selected.id, cell);
            }
        }

        /// <summary>The pointer of the map: what a click on the cell would do.</summary>
        private CursorKind CursorFor(int cell, HeroState hero)
        {
            GameState state = Game.State;
            HeroState standing = state.HeroAt(cell);
            MapObject what = state.ObjectAt(cell);
            if (standing != null && standing.owner == manager.Viewer)
            {
                return CursorKind.Hand;
            }
            if (what != null && what.kind == ObjectKind.Town && state.Town(what.subtype) is TownState here &&
                here.owner == manager.Viewer && (hero == null || hero.cell != cell))
            {
                return CursorKind.Hand;
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
            manager.Art.UseCursor(kind);
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
                TooltipBox.Show(this, heading, body, picture);
            }
            else
            {
                TooltipBox.Hide(this);
            }
        }
    }
}
