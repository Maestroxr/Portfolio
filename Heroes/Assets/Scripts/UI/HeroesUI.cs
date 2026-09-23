using System.Collections;
using System.Collections.Generic;
using System.Text;
using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// The interface of the game. It builds itself when a scenario starts, because what it has to show depends on the
    /// scenario: the resources of the player along the top, his heroes and towns down the right, the log and the
    /// minimap at the bottom, and over the map the screens of a town, of a hero and of a battle. It also reads the
    /// pointer on the map: what it is over, the trail it would take, and the order a click gives.
    /// </summary>
    public class HeroesUI : GameUI
    {
        private HeroesGameManager manager;
        private Canvas canvas;
        private RectTransform screen;

        private readonly TextMeshProUGUI[] resources = new TextMeshProUGUI[ResourceSet.Kinds];
        private TextMeshProUGUI date;
        private TextMeshProUGUI playerName;
        private RectTransform heroColumn;
        private RectTransform logContent;
        private ScrollRect logScroll;
        private Button endTurn;
        private Button nextHero;
        private Button sleepHero;
        private Button heroBook;
        private Button townBook;
        private RectTransform announce;
        private TextMeshProUGUI announceTitle;
        private TextMeshProUGUI announceBody;
        private Coroutine announcing;

        private RectTransform topBar;
        private RectTransform heroFrame;
        private RectTransform commands;
        private RectTransform logFrame;
        private RectTransform minimapFrame;
        private CampaignScreen campaignScreen;
        private TownScreen town;
        private HeroScreen heroSheet;
        private BattleBar battleBar;
        private ChoiceBox choice;
        private ResultsBox results;
        private Minimap minimap;

        private readonly List<HeroButton> heroButtons = new List<HeroButton>();
        private bool busy;
        private int hoverCell = -1;
        private MovePlan plan;

        /// <summary>The width a line of the log has inside its panel.</summary>
        private const float LogWidth = 484f;

        public bool Busy => busy;

        public TownScreen Town => town;

        public HeroScreen Sheet => heroSheet;

        public BattleBar Bar => battleBar;

        private HeroesGame Game => manager != null ? manager.Game : null;

        private IHeroesCommands Commands => manager != null ? manager.Commands : null;

        // ------------------------------------------------------------------ building

        /// <summary>
        /// Builds the whole interface once, the first time anything needs it: the screen of a scenario and the screens
        /// that come before one. Everything in it is filled from the game as it goes, so it is never rebuilt.
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
            canvas = MakeCanvas();
            screen = (RectTransform)canvas.transform;
            TopBar();
            RightColumn();
            Buttons();
            LogPanel();
            Announcement();

            town = TownScreen.Make(screen, manager);
            heroSheet = HeroScreen.Make(screen, manager);
            battleBar = BattleBar.Make(screen, manager);
            choice = ChoiceBox.Make(screen, manager);
            results = ResultsBox.Make(screen, manager);
            campaignScreen = CampaignScreen.Make(screen, manager);
            Tooltip.Prepare(screen);
            CloseAll();
            ShowHud(false);
        }

        /// <summary>A scenario is starting: the panels are cleared and pointed at it.</summary>
        public void Bind(HeroesGameManager owner)
        {
            manager = owner;
            Prepare();
            for (int i = logContent.childCount - 1; i >= 0; i--)
            {
                Destroy(logContent.GetChild(i).gameObject);
            }
            CloseAll();
            ShowHud(true);
            Log(manager.Scenario != null ? manager.Scenario.Goal : "Take the land.", -1);
        }

        /// <summary>The panels of a scenario are hidden while there is none.</summary>
        private void ShowHud(bool on)
        {
            foreach (RectTransform part in new[] { topBar, heroFrame, commands, logFrame, minimapFrame })
            {
                if (part != null)
                {
                    part.gameObject.SetActive(on);
                }
            }
        }

        /// <summary>The shared menu's New Game opens the campaign instead of starting one straight away.</summary>
        protected override void OnStartNewGameClicked()
        {
            if (!OpenCampaign())
            {
                base.OnStartNewGameClicked();
            }
        }

        /// <summary>Shows the chapters and the skirmish maps. False when the interface is not up yet.</summary>
        public bool OpenCampaign()
        {
            Prepare();
            if (campaignScreen == null)
            {
                return false;
            }
            HideMenu();
            campaignScreen.Show();
            return true;
        }

        private Canvas MakeCanvas()
        {
            var go = new GameObject("HeroesCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var made = go.GetComponent<Canvas>();
            made.renderMode = RenderMode.ScreenSpaceOverlay;
            made.sortingOrder = 10;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            if (EventSystem.current == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
            return made;
        }

        /// <summary>The seven resources and the date, on the bar along the top.</summary>
        private void TopBar()
        {
            Image bar = UIKit.Frame(screen, "TopBar");
            RectTransform rect = UIKit.Pin((RectTransform)bar.transform, new Vector2(0.5f, 1f), new Vector2(0f, 0f),
                new Vector2(1500f, 74f));
            rect.pivot = new Vector2(0.5f, 1f);
            topBar = rect;

            RectTransform row = UIKit.Rect(rect, "Resources");
            UIKit.Stretch(row, 26f, 8f, 320f, 8f);
            HorizontalLayoutGroup layout = UIKit.Layout<HorizontalLayoutGroup>(row, 6f);
            layout.childForceExpandWidth = true;
            layout.childAlignment = TextAnchor.MiddleLeft;
            for (int i = 0; i < ResourceSet.Kinds; i++)
            {
                var kind = (ResourceKind)i;
                resources[i] = UIKit.Resource(row, kind, 40f);
                UIKit.Fit((RectTransform)resources[i].transform.parent, 0f, 44f, true);
                Tooltip.Attach(resources[i].transform.parent.gameObject, Land.ResourceName(kind));
            }

            date = UIKit.Label(rect, "Date", "", 26f, UIKit.Gold, TextAlignmentOptions.MidlineRight);
            UIKit.Pin((RectTransform)date.transform, new Vector2(1f, 0.5f), new Vector2(-26f, 12f), new Vector2(290f, 30f));
            playerName = UIKit.Label(rect, "Player", "", 22f, UIKit.Dim, TextAlignmentOptions.MidlineRight);
            UIKit.Pin((RectTransform)playerName.transform, new Vector2(1f, 0.5f), new Vector2(-26f, -16f), new Vector2(290f, 26f));
        }

        /// <summary>The heroes and towns of the player, down the right hand side.</summary>
        private void RightColumn()
        {
            Image frame = UIKit.Frame(screen, "Heroes");
            RectTransform rect = UIKit.Pin((RectTransform)frame.transform, new Vector2(1f, 1f), new Vector2(-14f, -88f),
                new Vector2(250f, 620f));
            heroFrame = rect;
            heroColumn = UIKit.Rect(rect, "Column");
            UIKit.Stretch(heroColumn, 16f, 16f, 16f, 16f);
            VerticalLayoutGroup layout = UIKit.Layout<VerticalLayoutGroup>(heroColumn, 6f);
            layout.childForceExpandWidth = true;
            layout.childAlignment = TextAnchor.UpperCenter;
        }

        /// <summary>The commands of a turn, in the corner the eye goes to last.</summary>
        private void Buttons()
        {
            RectTransform holder = UIKit.Rect(screen, "Commands");
            UIKit.Pin(holder, new Vector2(1f, 0f), new Vector2(-14f, 14f), new Vector2(250f, 190f));
            commands = holder;

            endTurn = UIKit.Push(holder, "EndTurn", "End Turn", () => Commands?.EndTurn(), 28f);
            UIKit.Pin((RectTransform)endTurn.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(250f, 64f));

            RectTransform row = UIKit.Rect(holder, "Row");
            UIKit.Pin(row, new Vector2(0f, 0f), new Vector2(0f, 74f), new Vector2(250f, 62f));
            HorizontalLayoutGroup layout = UIKit.Layout<HorizontalLayoutGroup>(row, 8f);
            layout.childAlignment = TextAnchor.MiddleCenter;

            HeroesArt art = manager.Art;
            nextHero = UIKit.Icon(row, "NextHero", art.Icon("next_hero"), () => manager.Select(manager.NextHero()), "Next hero");
            UIKit.Fit((RectTransform)nextHero.transform, 56f, 56f);
            sleepHero = UIKit.Icon(row, "Sleep", art.Icon("sleep"), ToggleSleep, "Sleep this hero");
            UIKit.Fit((RectTransform)sleepHero.transform, 56f, 56f);
            heroBook = UIKit.Icon(row, "Hero", art.Icon("hero"), OpenHero, "Hero");
            UIKit.Fit((RectTransform)heroBook.transform, 56f, 56f);
            townBook = UIKit.Icon(row, "Town", art.Icon("kingdom"), OpenTown, "Town");
            UIKit.Fit((RectTransform)townBook.transform, 56f, 56f);

            Button menu = UIKit.Icon(holder, "Menu", art.Icon("menu"), ShowMenu, "Menu");
            UIKit.Pin((RectTransform)menu.transform, new Vector2(1f, 0f), new Vector2(0f, 142f), new Vector2(48f, 48f));
        }

        /// <summary>The running account of what happened, and the little map beside it.</summary>
        private void LogPanel()
        {
            Image frame = UIKit.Frame(screen, "Log");
            RectTransform rect = UIKit.Pin((RectTransform)frame.transform, new Vector2(0f, 0f), new Vector2(14f, 14f),
                new Vector2(520f, 180f));
            logFrame = rect;
            logContent = UIKit.Scroll(rect, "Scroll", out logScroll);
            UIKit.Stretch((RectTransform)logScroll.transform, 18f, 18f, 18f, 18f);
            VerticalLayoutGroup layout = UIKit.Layout<VerticalLayoutGroup>(logContent, 2f);
            layout.childForceExpandWidth = true;

            Image mapFrame = UIKit.Frame(screen, "Minimap");
            RectTransform mapRect = UIKit.Pin((RectTransform)mapFrame.transform, new Vector2(0f, 0f), new Vector2(548f, 14f),
                new Vector2(228f, 228f));
            minimapFrame = mapRect;
            minimap = Minimap.Make(mapRect, manager);
        }

        private void Announcement()
        {
            Image frame = UIKit.Parchment(screen, "Announce");
            announce = UIKit.Pin((RectTransform)frame.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 220f),
                new Vector2(620f, 130f));
            announceTitle = UIKit.Title(announce, "Title", "", 40f, UIKit.Ink * 0.25f);
            UIKit.Pin((RectTransform)announceTitle.transform, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(560f, 46f));
            announceBody = UIKit.Label(announce, "Body", "", 26f, new Color(0.25f, 0.18f, 0.1f), TextAlignmentOptions.Top);
            UIKit.Pin((RectTransform)announceBody.transform, new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(560f, 44f));
            announce.gameObject.SetActive(false);
        }

        protected override void Awake()
        {
            base.Awake();
            Prepare();
        }

        // ------------------------------------------------------------------ what the manager asks for

        public void CloseAll()
        {
            campaignScreen?.Close();
            town?.Close();
            heroSheet?.Close();
            choice?.Close();
            results?.Close();
            battleBar?.Hide();
            if (announce != null)
            {
                announce.gameObject.SetActive(false);
            }
        }

        public void SetBusy(bool on)
        {
            busy = on;
            if (on)
            {
                manager.Path?.Clear();
            }
            Refresh();
        }

        /// <summary>Redraws everything that shows a number: resources, the date, the heroes, the little map.</summary>
        public void Refresh()
        {
            HeroesGame game = Game;
            if (game == null || canvas == null)
            {
                return;
            }
            PlayerState viewer = manager.ViewerState;
            for (int i = 0; i < ResourceSet.Kinds; i++)
            {
                resources[i].text = viewer != null ? Short(viewer.resources.values[i]) : "0";
            }
            date.text = $"Month {game.State.Month}, Week {game.State.WeekOfMonth}, Day {game.State.DayOfWeek}";
            PlayerState current = game.State.Player(game.State.currentPlayer);
            playerName.text = current != null ? current.name : "";
            playerName.color = current != null ? HeroesArt.PlayerColor((int)current.color) : UIKit.Dim;

            RefreshHeroes(viewer);
            bool mine = current != null && current.index == manager.Viewer && !busy && !game.InBattle;
            UIKit.Enable(endTurn, mine);
            UIKit.Enable(nextHero, mine && manager.NextHero() != null);
            UIKit.Enable(sleepHero, mine && manager.Selected != null);
            UIKit.Enable(heroBook, manager.Selected != null);
            UIKit.Enable(townBook, viewer != null && viewer.towns.Count > 0);
            town?.Refresh();
            heroSheet?.Refresh();
            minimap?.Refresh();
        }

        private static string Short(int amount)
        {
            return amount >= 100000 ? $"{amount / 1000}k" : amount.ToString();
        }

        private void RefreshHeroes(PlayerState viewer)
        {
            var wanted = new List<(bool isHero, int id)>();
            if (viewer != null)
            {
                foreach (int id in viewer.heroes)
                {
                    wanted.Add((true, id));
                }
                foreach (int id in viewer.towns)
                {
                    wanted.Add((false, id));
                }
            }
            while (heroButtons.Count < wanted.Count)
            {
                heroButtons.Add(HeroButton.Make(heroColumn, manager, this));
            }
            for (int i = 0; i < heroButtons.Count; i++)
            {
                if (i < wanted.Count)
                {
                    heroButtons[i].Show(wanted[i].isHero, wanted[i].id);
                }
                else
                {
                    heroButtons[i].gameObject.SetActive(false);
                }
            }
        }

        public void Log(string text, int player)
        {
            if (string.IsNullOrEmpty(text) || logContent == null)
            {
                return;
            }
            Color color = player >= 0 ? HeroesArt.PlayerColor(PlayerColorOf(player)) : UIKit.Dim;
            TextMeshProUGUI line = UIKit.Label(logContent, "Line", text, 21f, color);
            line.textWrappingMode = TextWrappingModes.Normal;
            // The width is fixed to the panel, so a long line wraps rather than growing out of it.
            UIKit.Fit((RectTransform)line.transform, LogWidth, 0f);
            var element = line.GetComponent<LayoutElement>();
            element.preferredHeight = -1f;
            element.minHeight = 24f;
            if (logContent.childCount > 60)
            {
                Destroy(logContent.GetChild(0).gameObject);
            }
            StartCoroutine(ScrollDown());
        }

        private int PlayerColorOf(int player)
        {
            PlayerState state = Game?.State.Player(player);
            return state != null ? (int)state.color : -1;
        }

        private IEnumerator ScrollDown()
        {
            yield return null;
            if (logScroll != null)
            {
                logScroll.verticalNormalizedPosition = 0f;
            }
        }

        /// <summary>The banner that names the day, the week or the town that just changed hands.</summary>
        public void Announce(string title, string body)
        {
            if (announce == null)
            {
                return;
            }
            announceTitle.text = title;
            announceBody.text = body;
            announce.gameObject.SetActive(true);
            if (announcing != null)
            {
                StopCoroutine(announcing);
            }
            announcing = StartCoroutine(HideAnnounce());
        }

        private IEnumerator HideAnnounce()
        {
            yield return new WaitForSeconds(1.9f);
            float t = 0f;
            var group = announce.GetComponent<CanvasGroup>() ?? announce.gameObject.AddComponent<CanvasGroup>();
            while (t < 1f)
            {
                t += Time.deltaTime * 2.5f;
                group.alpha = 1f - t;
                yield return null;
            }
            group.alpha = 1f;
            announce.gameObject.SetActive(false);
            announcing = null;
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

        private void ToggleSleep()
        {
            HeroState hero = manager.Selected;
            if (hero != null)
            {
                Commands?.Sleep(hero.id, !hero.sleeping);
            }
        }

        // ------------------------------------------------------------------ the pointer on the map

        private void Update()
        {
            if (manager == null || Game == null || !manager.IsGameRunning)
            {
                return;
            }
            if (Game.InBattle)
            {
                battleBar?.Hover(PointerCell());
                return;
            }
            AdventureHover();
        }

        /// <summary>The cell the pointer is over, or -1 when it is over the interface or off the map.</summary>
        private int PointerCell()
        {
            if (manager.Rig == null || EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return -1;
            }
            return manager.Rig.PointerGround(Input.mousePosition, out Vector3 point, manager.Map.GroundMask)
                ? manager.Map.CellAt(point)
                : -1;
        }

        private void AdventureHover()
        {
            int cell = busy ? -1 : PointerCell();
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
                    manager.Path.Clear();
                }
            }
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
            if (manager.Selected != null && plan != null && plan.Cells.Count > 0)
            {
                Commands?.MoveHero(manager.Selected.id, cell);
            }
        }

        // ------------------------------------------------------------------ a line of text for anything

        /// <summary>What a cell holds, as the tooltip and the log say it.</summary>
        public string Describe(int cell)
        {
            GameState state = Game.State;
            var text = new StringBuilder();
            HeroState hero = state.HeroAt(cell);
            if (hero != null)
            {
                PlayerState owner = state.Player(hero.owner);
                text.Append(hero.Name).Append(", ").Append(HeroData.Class(hero.Def != null ? hero.Def.Class : HeroClass.Knight).Name);
                if (owner != null)
                {
                    text.Append(" (").Append(owner.name).Append(')');
                }
                return text.ToString();
            }
            MapObject what = state.ObjectAt(cell);
            if (what != null)
            {
                if (what.kind == ObjectKind.Town)
                {
                    TownState here = state.Town(what.subtype);
                    return here != null ? $"{here.name}, a {Land.FactionName(here.faction)} town" : "Town";
                }
                if (what.kind == ObjectKind.Monster)
                {
                    CreatureDef def = Creatures.Get(what.subtype);
                    return def != null ? $"{what.amount} {(what.amount == 1 ? def.Name : def.Plural)}" : "A wandering army";
                }
                return MapObjects.Name(what.kind);
            }
            return Land.Name(state.map.TerrainAt(cell));
        }
    }
}
