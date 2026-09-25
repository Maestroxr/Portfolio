using System.Collections;
using System.Collections.Generic;
using Gamebox.Online;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// What frames the adventure map: the treasury and the date on the bar along the top; the column of the original
    /// game down the right with the little map, the heroes, the towns, the commands and End Turn; the log at the bottom
    /// left, a line or two of parchment that opens into the whole account; and the banner that names a new day.
    /// </summary>
    public sealed class AdventureHud : MonoBehaviour
    {
        public const float ColumnWidth = 316f;
        public const float Margin = 8f;
        public const float TopHeight = 64f;
        private const int HeroRows = 4;
        private const int TownRows = 2;
        private const int LogLines = 60;
        /// <summary>The log folded shows its last two lines; opened, a dozen.</summary>
        private const float LogLine = 26f;
        private const float LogClosed = 2f * LogLine + 44f;
        private const float LogOpen = 12f * LogLine + 44f;
        private const float LogWidth = 760f;
        private const float EntryWidth = 146f;

        private HeroesGameManager manager;
        private HeroesUI ui;

        private RectTransform top;
        private readonly TextMeshProUGUI[] amounts = new TextMeshProUGUI[ResourceSet.Kinds];
        private readonly float[] shownAmounts = new float[ResourceSet.Kinds];
        private readonly int[] amountTargets = new int[ResourceSet.Kinds];
        private readonly Tooltip[] amountTips = new Tooltip[ResourceSet.Kinds];
        private TextMeshProUGUI date;
        private TextMeshProUGUI turn;
        private TextMeshProUGUI clock;
        private int clockShown = -1;
        private Image turnPennant;

        private RectTransform column;
        private Minimap minimap;
        private RectTransform heroList;
        private LayoutElement heroArea;
        private ScrollRect heroScroll;
        private TextMeshProUGUI noHeroes;
        private RectTransform townList;
        private LayoutElement townArea;
        private ScrollRect townScroll;
        private TextMeshProUGUI noTowns;
        private readonly List<HeroButton> heroButtons = new List<HeroButton>();
        private readonly List<TownButton> townButtons = new List<TownButton>();
        private Button nextHero;
        private Button sleep;
        private Button heroBook;
        private Button townBook;
        private Button endTurn;
        private Image endTurnGlow;
        private Tooltip sleepTip;
        private Tooltip menuTip;
        private bool pulse;
        private float nextRefresh;

        private RectTransform log;
        private RectTransform logContent;
        private ScrollRect logScroll;
        private TextMeshProUGUI logArrow;
        private bool logOpen;

        private RectTransform announce;
        private CanvasGroup announceGroup;
        private TextMeshProUGUI announceTitle;
        private TextMeshProUGUI announceBody;
        private Coroutine announcing;

        /// <summary>The top of the map left free by the bar along the top, from the top of the screen.</summary>
        public static float TopInset => Margin + TopHeight;

        public static AdventureHud Make(RectTransform parent, HeroesGameManager manager, HeroesUI ui)
        {
            RectTransform root = UIKit.Stretch(UIKit.Rect(parent, "Adventure"));
            var hud = root.gameObject.AddComponent<AdventureHud>();
            hud.manager = manager;
            hud.ui = ui;
            hud.TopBar(root);
            hud.Column(root);
            hud.LogPanel(root);
            hud.Announcement(root);
            return hud;
        }

        // ------------------------------------------------------------------ building

        /// <summary>The seven resources on the left of the bar, whose turn it is and the date on its right.</summary>
        private void TopBar(RectTransform root)
        {
            Image strip = UIKit.Strip(root, "TopBar");
            top = (RectTransform)strip.transform;
            top.anchorMin = new Vector2(0f, 1f);
            top.anchorMax = new Vector2(1f, 1f);
            top.pivot = new Vector2(0.5f, 1f);
            top.offsetMin = new Vector2(Margin, -Margin - TopHeight);
            top.offsetMax = new Vector2(-Margin, -Margin);
            RectTransform content = UIKit.Content(strip, 2f);

            RectTransform row = UIKit.Rect(content, "Resources");
            row.anchorMin = new Vector2(0f, 0f);
            row.anchorMax = new Vector2(0f, 1f);
            row.pivot = new Vector2(0f, 0.5f);
            row.offsetMin = new Vector2(10f, 0f);
            row.offsetMax = new Vector2(10f + EntryWidth * ResourceSet.Kinds, 0f);
            HorizontalLayoutGroup layout = UIKit.Layout<HorizontalLayoutGroup>(row, 0f);
            layout.childAlignment = TextAnchor.MiddleLeft;
            for (int i = 0; i < ResourceSet.Kinds; i++)
            {
                var kind = (ResourceKind)i;
                amounts[i] = UIKit.Resource(row, kind, 36f);
                UIKit.FitLine(amounts[i], 23f, 15f);
                UIKit.Look(amounts[i], TextLook.Shadow);
                var entry = (RectTransform)amounts[i].transform.parent;
                UIKit.Fit(entry, EntryWidth, 40f);
                amountTips[i] = Tooltip.Attach(entry.gameObject, Land.ResourceName(kind), "");
                // The whole entry answers the pointer, not only its icon.
                entry.gameObject.AddComponent<Image>().color = Color.clear;
            }

            Image dateBox = UIKit.Recess(content, "Date");
            UIKit.Pin((RectTransform)dateBox.transform, new Vector2(1f, 0.5f), new Vector2(-4f, 0f), new Vector2(330f, 42f));
            date = UIKit.Label(dateBox.transform, "Text", "", 22f, UIKit.Gold, TextAlignmentOptions.Center, true);
            UIKit.Stretch((RectTransform)date.transform, 10f, 0f, 10f, 0f);
            UIKit.FitLine(date, 21f, 14f);
            date.characterSpacing = 1f;

            Image turnBox = UIKit.Recess(content, "Turn");
            UIKit.Pin((RectTransform)turnBox.transform, new Vector2(1f, 0.5f), new Vector2(-346f, 0f), new Vector2(270f, 42f));
            turnPennant = UIKit.Pennant(turnBox.transform, "Pennant", Color.white);
            UIKit.Pin((RectTransform)turnPennant.transform, new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(18f, 34f));
            turn = UIKit.Label(turnBox.transform, "Text", "", 21f, UIKit.Ink, TextAlignmentOptions.Center);
            UIKit.Stretch((RectTransform)turn.transform, 36f, 0f, 10f, 0f);
            UIKit.FitLine(turn, 21f, 13f);
            // Online, the time left on the clock of the room, at the right end of the box.
            clock = UIKit.Label(turnBox.transform, "Clock", "", 21f, UIKit.Gold, TextAlignmentOptions.MidlineRight, true);
            UIKit.Pin((RectTransform)clock.transform, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(66f, 34f));
            UIKit.FitLine(clock, 21f, 14f);
            clock.gameObject.SetActive(false);
        }

        /// <summary>The column down the right: the little map, the heroes, the towns, the commands, End Turn.</summary>
        private void Column(RectTransform root)
        {
            Image frame = UIKit.Frame(root, "Column");
            column = UIKit.Pin((RectTransform)frame.transform, new Vector2(1f, 1f), new Vector2(-Margin, -(TopInset + 6f)),
                new Vector2(ColumnWidth, 600f));
            RectOffset padding = UIKit.Padding(frame, 4f);
            VerticalLayoutGroup layout = UIKit.Layout<VerticalLayoutGroup>(column, 8f, padding);
            layout.childForceExpandWidth = true;
            layout.childAlignment = TextAnchor.UpperCenter;
            column.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            float inner = ColumnWidth - padding.horizontal;

            minimap = Minimap.Make(column, manager, inner);
            Rule(column);

            heroList = List(column, "Heroes", out heroScroll, out heroArea);
            noHeroes = Empty(column, "No heroes. Hire one in a tavern.");
            townList = List(column, "Towns", out townScroll, out townArea);
            noTowns = Empty(column, "No towns.");
            Rule(column);

            RectTransform commands = UIKit.Rect(column, "Commands");
            UIKit.Fit(commands, 0f, 50f, true);
            HorizontalLayoutGroup row = UIKit.Layout<HorizontalLayoutGroup>(commands, 4f);
            row.childAlignment = TextAnchor.MiddleCenter;
            HeroesArt art = manager.Art;
            nextHero = Command(commands, "NextHero", art.Icon("next_hero"), () => manager.Select(manager.NextHero()),
                "Next Hero\nThe next hero who can still move.");
            sleep = Command(commands, "Sleep", art.Icon("sleep"), ToggleSleep, "Sleep\nNext Hero passes a sleeping hero by.");
            sleepTip = sleep.GetComponent<Tooltip>();
            heroBook = Command(commands, "Hero", art.Icon("hero"), ui.OpenHero, "Hero\nThe book of the hero in hand.");
            townBook = Command(commands, "Town", art.Icon("kingdom"), ui.OpenTown, "Town\nThe screen of your first town.");
            menuTip = Command(commands, "Menu", art.Icon("menu"), ui.OpenMenu, "Menu\nSave, load, settings, leave.").GetComponent<Tooltip>();

            RectTransform holder = UIKit.Rect(column, "EndTurn");
            UIKit.Fit(holder, 0f, 62f, true);
            endTurnGlow = UIKit.Halo(holder, "Glow", new Color(1f, 0.82f, 0.4f, 0f));
            UIKit.Stretch((RectTransform)endTurnGlow.transform, -34f, -26f, -34f, -26f);
            endTurn = UIKit.Push(holder, "Button", "End Turn", () => ui.EndTurn(), 28f);
            UIKit.Stretch((RectTransform)endTurn.transform);
            UIKit.Look(endTurn.GetComponentInChildren<TextMeshProUGUI>(), TextLook.Gold);
            Tooltip.Attach(endTurn.gameObject, "End Turn\nThe day ends for your realm; the others move.");
        }

        private static void Rule(RectTransform parent)
        {
            RectTransform rule = UIKit.Divider(parent, "Rule", 14f);
            UIKit.Fit(rule, 0f, 14f, true);
        }

        /// <summary>A list of cards that scrolls once it holds more than fit.</summary>
        private static RectTransform List(RectTransform parent, string name, out ScrollRect scroll, out LayoutElement area)
        {
            RectTransform content = UIKit.Scroll(parent, name, out scroll);
            area = scroll.gameObject.AddComponent<LayoutElement>();
            VerticalLayoutGroup layout = UIKit.Layout<VerticalLayoutGroup>(content, 4f);
            layout.childForceExpandWidth = true;
            return content;
        }

        private static TextMeshProUGUI Empty(RectTransform parent, string text)
        {
            TextMeshProUGUI label = UIKit.Label(parent, "Empty", text, 18f, UIKit.Dim, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Italic;
            UIKit.Fit((RectTransform)label.transform, 0f, 28f, true);
            return label;
        }

        private static Button Command(RectTransform parent, string name, Sprite icon, System.Action onClick, string tip)
        {
            Button button = UIKit.Icon(parent, name, icon, onClick, tip);
            UIKit.Fit((RectTransform)button.transform, 50f, 50f);
            return button;
        }

        /// <summary>The account of what happened, on a strip of parchment at the bottom left; a click opens it.</summary>
        private void LogPanel(RectTransform root)
        {
            Image frame = UIKit.Parchment(root, "Log");
            frame.raycastTarget = true;
            log = UIKit.Pin((RectTransform)frame.transform, new Vector2(0f, 0f), new Vector2(Margin, Margin),
                new Vector2(LogWidth, LogClosed));
            var button = frame.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(ToggleLog);
            frame.gameObject.AddComponent<Clicker>();
            // The corners of the parchment are drawn a little further in than its edges: the words keep clear of them.
            logContent = UIKit.Scroll(log, "Lines", out logScroll, false);
            UIKit.Stretch((RectTransform)logScroll.transform, 34f, 22f, 44f, 22f);
            VerticalLayoutGroup layout = UIKit.Layout<VerticalLayoutGroup>(logContent, 0f);
            layout.childForceExpandWidth = true;
            layout.childAlignment = TextAnchor.LowerLeft;
            logArrow = UIKit.Label(log, "Arrow", "▲", 18f, UIKit.DimOnParchment, TextAlignmentOptions.Center);
            UIKit.Pin((RectTransform)logArrow.transform, new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(24f, 24f));
            Tooltip.Attach(frame.gameObject, "The Chronicle\nClick to read further back.");
        }

        /// <summary>The ribbon that names a new day or week, with a line on parchment under it.</summary>
        private void Announcement(RectTransform root)
        {
            announce = UIKit.Pin(UIKit.Rect(root, "Announce"), new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(720f, 176f));
            announce.pivot = new Vector2(0.5f, 0.5f);
            announceGroup = announce.gameObject.AddComponent<CanvasGroup>();
            announceGroup.blocksRaycasts = false;
            announceGroup.interactable = false;
            Image plate = UIKit.Parchment(announce, "Plate");
            UIKit.Pin((RectTransform)plate.transform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(600f, 110f));
            announceBody = UIKit.Label(plate.transform, "Body", "", 26f, UIKit.InkOnParchment, TextAlignmentOptions.Center);
            UIKit.Stretch((RectTransform)announceBody.transform, 34f, 18f, 34f, 34f);
            UIKit.FitLine(announceBody, 26f, 16f);
            Image ribbon = UIKit.Ribbon(announce, "Ribbon", "", 38f);
            UIKit.Pin((RectTransform)ribbon.transform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(700f, 92f));
            announceTitle = ribbon.GetComponentInChildren<TextMeshProUGUI>();
            UIKit.FitLine(announceTitle, 38f, 22f);
            announce.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ what the interface asks for

        public void Show(bool on)
        {
            top.gameObject.SetActive(on);
            column.gameObject.SetActive(on);
            log.gameObject.SetActive(on);
            if (!on)
            {
                HideAnnouncement();
            }
        }

        public bool IsShown => column.gameObject.activeSelf;

        /// <summary>Redraws the numbers: the treasury, the date, whose turn, the heroes and towns, the commands.</summary>
        public void Refresh(bool busy)
        {
            HeroesGame game = manager.Game;
            if (game == null)
            {
                return;
            }
            GameState state = game.State;
            PlayerState viewer = manager.ViewerState;
            for (int i = 0; i < ResourceSet.Kinds; i++)
            {
                int amount = viewer != null ? viewer.resources.values[i] : 0;
                if (amountTargets[i] != amount || amounts[i].text.Length == 0)
                {
                    amountTargets[i] = amount;
                    amountTips[i].Text = $"{amount:N0} in the treasury." + (i == 0 ? $"\nYour towns bring {Income(viewer)} a day." : "");
                }
            }
            date.text = $"Month {state.Month}, Week {state.WeekOfMonth}, Day {state.DayOfWeek}";
            PlayerState current = state.Player(state.currentPlayer);
            bool mine = current != null && current.index == manager.Viewer;
            turnPennant.color = HeroesArt.PlayerColor(current != null ? (int)current.color : 4);
            turn.text = current == null ? "" : mine ? "Your turn" : $"{current.name} {(busy ? "moves" : "thinks")}";
            turn.color = mine ? UIKit.Good : UIKit.Ink;

            Lists(viewer);
            bool free = mine && !busy && !game.InBattle;
            HeroState next = manager.NextHero();
            UIKit.Enable(endTurn, free);
            UIKit.Enable(nextHero, free && next != null);
            UIKit.Enable(sleep, free && manager.Selected != null);
            UIKit.Enable(heroBook, manager.Selected != null);
            UIKit.Enable(townBook, viewer != null && viewer.towns.Count > 0);
            HeroState selected = manager.Selected;
            if (sleepTip != null)
            {
                sleepTip.Text = selected != null && selected.sleeping ? "Wake the hero in hand." : "Put the hero in hand to sleep: Next Hero passes him by.";
                sleepTip.Title = selected != null && selected.sleeping ? "Wake Up" : "Sleep";
            }
            if (menuTip != null)
            {
                // Online the command opens the match menu of the room, which neither saves nor stops the game.
                menuTip.Text = manager.IsOnlineGame
                    ? "The match menu: back to the game, or leave the match. The game goes on meanwhile."
                    : "Save, load, settings, leave.";
                menuTip.Title = "Menu";
            }
            pulse = free && next == null;
            minimap.Refresh(true);
        }

        private int Income(PlayerState player)
        {
            if (player == null || manager.Game == null)
            {
                return 0;
            }
            int total = 0;
            foreach (int id in player.towns)
            {
                TownState town = manager.Game.State.Town(id);
                if (town != null)
                {
                    total += Buildings.Income(town);
                }
            }
            return total;
        }

        /// <summary>The heroes and towns of the player at this device, as cards, pooled.</summary>
        private void Lists(PlayerState viewer)
        {
            int heroes = viewer != null ? viewer.heroes.Count : 0;
            while (heroButtons.Count < heroes)
            {
                heroButtons.Add(HeroButton.Make(heroList, manager, ui));
            }
            for (int i = 0; i < heroButtons.Count; i++)
            {
                if (i < heroes)
                {
                    heroButtons[i].Show(viewer.heroes[i]);
                }
                else
                {
                    heroButtons[i].gameObject.SetActive(false);
                }
            }
            int towns = viewer != null ? viewer.towns.Count : 0;
            while (townButtons.Count < towns)
            {
                townButtons.Add(TownButton.Make(townList, manager, ui));
            }
            for (int i = 0; i < townButtons.Count; i++)
            {
                if (i < towns)
                {
                    townButtons[i].Show(viewer.towns[i]);
                }
                else
                {
                    townButtons[i].gameObject.SetActive(false);
                }
            }
            Size(heroArea, heroScroll, heroes, HeroRows, HeroButton.Height);
            Size(townArea, townScroll, towns, TownRows, TownButton.Height);
            noHeroes.gameObject.SetActive(heroes == 0);
            noTowns.gameObject.SetActive(towns == 0);
        }

        /// <summary>A list as tall as its cards, up to a number of them; past that it scrolls.</summary>
        private static void Size(LayoutElement area, ScrollRect scroll, int count, int most, float height)
        {
            int shown = Mathf.Min(count, most);
            float wanted = shown > 0 ? shown * (height + 4f) - 4f : 0f;
            area.preferredHeight = wanted;
            area.minHeight = wanted;
            area.gameObject.SetActive(count > 0);
            scroll.vertical = count > most;
        }

        private void ToggleSleep()
        {
            HeroState hero = manager.Selected;
            if (hero != null && ui.CanCommand)
            {
                manager.Commands?.Sleep(hero.id, !hero.sleeping);
            }
        }

        private void Update()
        {
            // The treasury counts up and down to what it holds rather than jumping.
            for (int i = 0; i < ResourceSet.Kinds; i++)
            {
                float target = amountTargets[i];
                if (!Mathf.Approximately(shownAmounts[i], target) || amounts[i].text.Length == 0)
                {
                    float step = Mathf.Max(1f, Mathf.Abs(target - shownAmounts[i]) * Time.unscaledDeltaTime * 6f);
                    shownAmounts[i] = Mathf.MoveTowards(shownAmounts[i], target, step);
                    amounts[i].text = Mathf.RoundToInt(shownAmounts[i]).ToString("N0");
                }
            }
            Clock();
            // End Turn glows when no hero has anything left to do.
            Color glow = endTurnGlow.color;
            glow.a = pulse ? 0.25f + 0.3f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.2f)) : 0f;
            endTurnGlow.color = glow;
            // The cards follow the movement and mana of their heroes.
            if (Time.unscaledTime >= nextRefresh && column.gameObject.activeInHierarchy)
            {
                nextRefresh = Time.unscaledTime + 0.25f;
                foreach (HeroButton button in heroButtons)
                {
                    if (button.isActiveAndEnabled)
                    {
                        button.Refresh();
                    }
                }
                minimap.Refresh();
            }
        }

        /// <summary>
        /// Online with a turn clock, the seconds left to whoever the game waits for, beside whose turn it is (in a battle,
        /// while this bar is hidden, the battle bar shows them); red for the last ten.
        /// </summary>
        private void Clock()
        {
            ServerClient server = manager.IsOnlineGame && manager.Online != null ? manager.Online.ServerClient : null;
            RoomTurnInfo timer = server != null ? server.Turn : null;
            bool timed = timer != null && timer.HasLimit;
            if (clock.gameObject.activeSelf != timed)
            {
                clock.gameObject.SetActive(timed);
                ((RectTransform)turn.transform).offsetMax = new Vector2(timed ? -82f : -10f, 0f);
                clockShown = -1;
            }
            if (!timed)
            {
                return;
            }
            int seconds = Mathf.CeilToInt(timer.Remaining);
            if (seconds != clockShown)
            {
                clockShown = seconds;
                clock.text = $"{seconds / 60}:{seconds % 60:00}";
                clock.color = seconds <= 10 ? UIKit.Bad : UIKit.Gold;
            }
        }

        // ------------------------------------------------------------------ the log

        public void ClearLog()
        {
            UIKit.Clear(logContent);
            SetLogOpen(false);
        }

        public void Log(string text, Color color)
        {
            TextMeshProUGUI line = UIKit.Label(logContent, "Line", text, 20f, color);
            Fold(line, logOpen);
            var element = line.gameObject.AddComponent<LayoutElement>();
            element.minHeight = LogLine;
            if (logContent.childCount > LogLines)
            {
                Destroy(logContent.GetChild(0).gameObject);
            }
            StartCoroutine(FadeIn(line));
        }

        /// <summary>Folds the log back to its last lines (a battle on the map needs the bottom of the screen).</summary>
        public void CloseLog()
        {
            SetLogOpen(false);
        }

        private void ToggleLog()
        {
            SetLogOpen(!logOpen);
        }

        /// <summary>A line of the folded log keeps to one line (cut short); opened, it wraps.</summary>
        private static void Fold(TextMeshProUGUI line, bool open)
        {
            line.textWrappingMode = open ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            line.overflowMode = open ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
        }

        /// <summary>Opens the log into the whole account, or folds it back to its last lines.</summary>
        public void SetLogOpen(bool open)
        {
            logOpen = open;
            foreach (Transform child in logContent)
            {
                if (child.TryGetComponent(out TextMeshProUGUI line))
                {
                    Fold(line, open);
                }
            }
            log.sizeDelta = new Vector2(LogWidth, open ? LogOpen : LogClosed);
            logArrow.text = open ? "▼" : "▲";
            logScroll.vertical = open;
            StartCoroutine(ScrollDown());
        }

        private IEnumerator FadeIn(TextMeshProUGUI line)
        {
            yield return ScrollDown();
            float alpha = 0f;
            while (line != null && alpha < 1f)
            {
                alpha = Mathf.MoveTowards(alpha, 1f, Time.unscaledDeltaTime * 3f);
                line.alpha = alpha;
                yield return null;
            }
        }

        private IEnumerator ScrollDown()
        {
            yield return null;
            if (logScroll != null)
            {
                logScroll.verticalNormalizedPosition = 0f;
            }
        }

        // ------------------------------------------------------------------ the banner

        public void Announce(string title, string body)
        {
            announceTitle.text = title;
            announceBody.text = body;
            announceBody.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(body));
            announce.gameObject.SetActive(true);
            announce.SetAsLastSibling();
            if (announcing != null)
            {
                StopCoroutine(announcing);
            }
            announcing = StartCoroutine(Announcing());
        }

        public void HideAnnouncement()
        {
            if (announcing != null)
            {
                StopCoroutine(announcing);
                announcing = null;
            }
            announce.gameObject.SetActive(false);
        }

        /// <summary>The banner grows in, stays a moment and fades away, in unscaled time so a pause does not hold it.</summary>
        private IEnumerator Announcing()
        {
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.MoveTowards(t, 1f, Time.unscaledDeltaTime / 0.3f);
                float eased = Gamebox.Tween.OutQuad(t);
                announceGroup.alpha = eased;
                announce.localScale = Vector3.one * Mathf.Lerp(0.86f, 1f, eased);
                yield return null;
            }
            float until = Time.unscaledTime + 1.8f;
            while (Time.unscaledTime < until)
            {
                yield return null;
            }
            while (t > 0f)
            {
                t = Mathf.MoveTowards(t, 0f, Time.unscaledDeltaTime / 0.5f);
                announceGroup.alpha = t;
                yield return null;
            }
            announce.gameObject.SetActive(false);
            announcing = null;
        }
    }
}
