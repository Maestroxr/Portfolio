using System;
using System.Collections;
using System.Collections.Generic;
using Gamebox.Online;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// The battle's control panel, the same for a battle on the map and on a battlefield of its own, in the manner of the
    /// old games: along the bottom the two heroes at the ends, the commands (options, retreat, auto combat on the left;
    /// the spellbook, wait and defend on the right) and the last lines of the combat log between them; above it the card
    /// of the stack whose turn it is, the order the others act in, and the round. On the field it paints where the stack
    /// in hand may go and what it may strike, rings the cell under the pointer, turns the pointer into what a click there
    /// would do (the sword of a blow points the way it is struck, from the side of the target the pointer is on) and
    /// tells the damage a blow, a shot or a spell would deal. A click becomes a command, and only while the rules wait for
    /// this device and the view has played out everything before: auto combat makes the same commands with the
    /// computer's battle sense, so it works in an online game as well.
    /// </summary>
    public sealed class BattleBar : MonoBehaviour
    {
        private const float PanelHeight = 170f;
        private const float BandHeight = 112f;
        private const int LogLines = 3;
        private const int QueueChips = 12;
        /// <summary>The width of the clock at the start of the round's line, which the round's number leaves free while it shows.</summary>
        private const float ClockWidth = 76f;

        private static readonly Color AutoOn = new Color(1f, 0.82f, 0.35f, 0.9f);

        private HeroesGameManager manager;
        private RectTransform root;
        private RectTransform screen;
        private readonly BattleHeroBlock[] heroes = new BattleHeroBlock[2];
        private BattleStackCard current;
        private RectTransform queue;
        private readonly List<BattleQueueChip> chips = new List<BattleQueueChip>();
        private RectTransform nextRoundMark;
        private TextMeshProUGUI round;
        private TextMeshProUGUI status;
        private TextMeshProUGUI clock;
        private int clockShown = -1;
        private Button options;
        private Button retreat;
        private Button auto;
        private Image autoHalo;
        private Button cast;
        private Button wait;
        private Button defend;
        private readonly TextMeshProUGUI[] logLines = new TextMeshProUGUI[LogLines];
        private readonly List<(string text, Color color)> log = new List<(string, Color)>();
        private BattleSpellbook spellbook;
        private BattleResults results;
        private BattleQuestion question;
        private BattleStackCard info;
        private BattlePointerTip tip;
        private BattleFade fade;
        private BattleBanner banner;
        private Func<bool> back;

        private Dictionary<int, int> reach = new Dictionary<int, int>();
        private readonly List<int> targets = new List<int>();
        private readonly List<int> shots = new List<int>();
        private readonly List<int> castable = new List<int>();
        private int computedFor = -1;
        private SpellId computedSpell = SpellId.None;
        private int computedAt = -1;
        private Dictionary<int, int> strikeReach;
        private int strikeBy = -1;
        private int strikeAt = -1;
        private List<int> strikeFrom;
        private (int, int, int, int, int) tipKey;
        private Dictionary<int, int> tipReach;
        private string tipText;
        private (int, int, int, int, int) cardKey;
        private bool cardDrawn;
        private readonly List<BattleStack> now = new List<BattleStack>();
        private readonly List<BattleStack> waiting = new List<BattleStack>();
        private readonly List<BattleStack> later = new List<BattleStack>();
        private List<int> queueDrawn = new List<int>();
        private List<int> queueSeen = new List<int>();
        private bool queueEmpty = true;
        private BattleState sorting;
        private Comparison<BattleStack> slowestFirst;
        private Comparison<BattleStack> fastestFirst;
        private bool autoCombat;
        private float autoNext;
        private int infoStack = -1;
        private int bookFor = -1;
        private int askedFor = -1;
        private BattleView subscribed;

        /// <summary>What a click on a cell of the field would do.</summary>
        private enum Act
        {
            Nothing,
            Move,
            Fly,
            Strike,
            Shoot,
            Cast,
            Blocked
        }

        public static BattleBar Make(Transform parent, HeroesGameManager manager)
        {
            RectTransform holder = UIKit.Rect(parent, "Battle");
            UIKit.Stretch(holder);
            var bar = holder.gameObject.AddComponent<BattleBar>();
            bar.manager = manager;
            bar.screen = (RectTransform)parent;
            bar.root = UIKit.Rect(holder, "Bar");
            UIKit.Stretch(bar.root);
            bar.Band();
            bar.Panel();
            bar.spellbook = BattleSpellbook.Make(holder, manager);
            bar.info = BattleStackCard.Make(holder, "Stack Info", true);
            UIKit.Pin((RectTransform)bar.info.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(620f, 330f));
            bar.info.gameObject.SetActive(false);
            bar.banner = BattleBanner.Make(holder);
            bar.results = BattleResults.Make(holder);
            bar.question = BattleQuestion.Make(holder);
            bar.tip = BattlePointerTip.Make(holder);
            bar.fade = BattleFade.Make(holder);
            bar.root.gameObject.SetActive(false);
            return bar;
        }

        /// <summary>The band over the panel: the card of the stack in hand, the order of the others, the round.</summary>
        private void Band()
        {
            Image band = UIKit.Sprite(root, "Band", UIKit.Art != null ? UIKit.Art.shade : null, new Color(1f, 1f, 1f, 0.92f));
            band.type = Image.Type.Simple;
            var rect = (RectTransform)band.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(0f, PanelHeight - 8f);
            rect.offsetMax = new Vector2(0f, PanelHeight + BandHeight);
            band.raycastTarget = true;

            current = BattleStackCard.Make(rect, "Current", false);
            var card = (RectTransform)current.transform;
            UIKit.Pin(card, new Vector2(0f, 0f), new Vector2(14f, 14f), new Vector2(600f, 100f));

            RectTransform roundBox = UIKit.Rect(rect, "Round");
            UIKit.Pin(roundBox, new Vector2(1f, 0f), new Vector2(-18f, 14f), new Vector2(230f, 96f));
            round = UIKit.Heading(roundBox, "Number", "Round 1", 34f, TextAlignmentOptions.Right);
            UIKit.FitLine(round, 34f, 22f);
            UIKit.Pin((RectTransform)round.transform, new Vector2(1f, 1f), new Vector2(0f, -8f), new Vector2(230f, 44f));
            status = UIKit.Label(roundBox, "Status", "", 20f, UIKit.Dim, TextAlignmentOptions.Right);
            UIKit.FitLine(status, 20f, 14f);
            status.fontStyle = FontStyles.Italic;
            UIKit.Pin((RectTransform)status.transform, new Vector2(1f, 1f), new Vector2(0f, -56f), new Vector2(230f, 30f));
            // Online with a clock on the turns, the time left at the start of the round's line (the lobby's turn banner
            // stays off in a game of heroes, whose bars show the turn and the clock).
            clock = UIKit.Label(roundBox, "Clock", "", 28f, UIKit.Gold, TextAlignmentOptions.MidlineLeft, true);
            UIKit.FitLine(clock, 28f, 18f);
            UIKit.Pin((RectTransform)clock.transform, new Vector2(0f, 1f), new Vector2(0f, -8f), new Vector2(ClockWidth, 44f));
            clock.gameObject.SetActive(false);

            queue = UIKit.Rect(rect, "Queue");
            queue.anchorMin = new Vector2(0f, 0f);
            queue.anchorMax = new Vector2(1f, 0f);
            queue.pivot = new Vector2(0f, 0f);
            queue.offsetMin = new Vector2(636f, 10f);
            queue.offsetMax = new Vector2(-262f, 100f);
            HorizontalLayoutGroup row = UIKit.Layout<HorizontalLayoutGroup>(queue, 6f);
            row.childAlignment = TextAnchor.LowerLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            for (int i = 0; i < QueueChips; i++)
            {
                chips.Add(BattleQueueChip.Make(queue));
            }
            nextRoundMark = UIKit.Rect(queue, "Next Round");
            UIKit.Fit(nextRoundMark, 22f, 86f);
            Image line = UIKit.Sprite(nextRoundMark, "Line", null, new Color(0.88f, 0.74f, 0.4f, 0.55f));
            UIKit.Pin((RectTransform)line.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 70f));
        }

        /// <summary>The panel along the bottom: the heroes, the commands and the log.</summary>
        private void Panel()
        {
            Image strip = UIKit.Strip(root, "Panel");
            var rect = (RectTransform)strip.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(0f, 0f);
            rect.offsetMax = new Vector2(0f, PanelHeight);
            RectTransform content = UIKit.Content(strip, 2f);
            heroes[0] = BattleHeroBlock.Make(content, true);
            heroes[1] = BattleHeroBlock.Make(content, false);

            RectTransform middle = UIKit.Rect(content, "Middle");
            UIKit.Stretch(middle, 420f, 0f, 420f, 0f);

            RectTransform left = UIKit.Rect(middle, "Left Commands");
            UIKit.Pin(left, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(252f, 118f));
            options = Command(left, "Options", "menu", "Options", "Esc", OpenMenu);
            retreat = Command(left, "Retreat", "retreat", "Retreat", "R", Retreat);
            auto = Command(left, "Auto", "auto", "Auto combat", "A", ToggleAuto);
            autoHalo = UIKit.Halo(auto.transform, "On", AutoOn);
            UIKit.Stretch((RectTransform)autoHalo.transform, -14f, -14f, -14f, -14f);
            autoHalo.transform.SetAsFirstSibling();
            autoHalo.gameObject.SetActive(false);
            Row(left);

            RectTransform right = UIKit.Rect(middle, "Right Commands");
            UIKit.Pin(right, new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(252f, 118f));
            cast = Command(right, "Cast", "spellbook", "Cast a spell", "S", OpenSpells);
            wait = Command(right, "Wait", "wait", "Wait", "W", Wait);
            defend = Command(right, "Defend", "defend", "Defend", "D", Defend);
            Row(right);

            Image logBox = UIKit.Recess(middle, "Log");
            var logRect = (RectTransform)logBox.transform;
            UIKit.Stretch(logRect, 272f, 18f, 272f, 18f);
            RectTransform lines = UIKit.Content(logBox, 8f);
            VerticalLayoutGroup column = UIKit.Layout<VerticalLayoutGroup>(lines, 2f);
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = true;
            column.childAlignment = TextAnchor.LowerLeft;
            for (int i = 0; i < LogLines; i++)
            {
                logLines[i] = UIKit.Label(lines, "Line", "", 19f, UIKit.Ink, TextAlignmentOptions.MidlineLeft);
                UIKit.FitLine(logLines[i], 19f, 13f);
            }
        }

        private static void Row(RectTransform group)
        {
            HorizontalLayoutGroup row = UIKit.Layout<HorizontalLayoutGroup>(group, 14f);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = false;
            row.childControlHeight = false;
        }

        /// <summary>A round command with its icon, its tooltip and the key that does the same, written under it.</summary>
        private Button Command(RectTransform parent, string name, string icon, string tip, string key, Action onClick)
        {
            RectTransform holder = UIKit.Rect(parent, name);
            holder.sizeDelta = new Vector2(74f, 110f);
            Button button = UIKit.Icon(holder, "Button", UIKit.Art != null ? UIKit.Art.Icon(icon) : null, onClick);
            UIKit.Pin((RectTransform)button.transform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(72f, 72f));
            Tooltip.Attach(button.gameObject, tip, $"Key: {key}");
            TextMeshProUGUI caption = UIKit.Label(holder, "Key", key, 18f, UIKit.Dim, TextAlignmentOptions.Center);
            caption.textWrappingMode = TextWrappingModes.NoWrap;
            UIKit.Pin((RectTransform)caption.transform, new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(74f, 26f));
            return button;
        }

        private HeroesGame Game => manager.Game;

        private BattleView View => manager.Battle;

        /// <summary>The battle as the view has shown it: what the bar tells of while events play.</summary>
        private BattleState Shown => View != null ? View.Shown : null;

        /// <summary>The spell the player picked and is now choosing a target for, or None.</summary>
        public SpellId Casting { get; private set; } = SpellId.None;

        /// <summary>Whether the computer makes this device's moves in the battle on the screen (until it ends).</summary>
        public bool AutoCombat => autoCombat;

        public BattleResults Results => results;

        /// <summary>Whether the bar is asking the player to confirm a step (a retreat) before it is taken.</summary>
        public bool Asking => question != null && question.IsOpen;

        /// <summary>
        /// Development tours only: a place on the screen (in pixels) the field is read at instead of the mouse's, so a
        /// tour can point at the field as a player would. Null: the mouse.
        /// </summary>
        public Vector3? TourPointer { get; set; }

        /// <summary>Development tours only: a click at <see cref="TourPointer"/>, taken on the next frame the field is read.</summary>
        public bool TourClick { get; set; }

        /// <summary>Development tours only: the right button held down at <see cref="TourPointer"/> (the card of a stack).</summary>
        public bool TourHold { get; set; }

        /// <summary>Where the pointer is: the mouse, or the place a tour points at.</summary>
        private Vector3 PointerAt => TourPointer ?? Input.mousePosition;

        // ------------------------------------------------------------------ the manager's side

        public void Begin(BattleState battle)
        {
            root.gameObject.SetActive(true);
            computedFor = -1;
            // Nothing drawn yet: the first look at the battle fills the card and the queue whatever they hold.
            cardDrawn = false;
            queueEmpty = true;
            Casting = SpellId.None;
            // Auto combat is for the battle it was turned on in, as in the old games: the next one waits for orders.
            autoCombat = false;
            // Whatever pointer the last battle or the map left up, this battle sets its own from the first frame.
            cursorNow = (CursorKind)(-1);
            log.Clear();
            DrawLog();
            Refresh();
            SetRound(Mathf.Max(1, battle.round));
            if (subscribed != View)
            {
                if (subscribed != null)
                {
                    subscribed.Told -= Log;
                }
                subscribed = View;
                subscribed.Told += Log;
            }
            if (back == null)
            {
                back = Back;
            }
            manager.AddBackHandler(back);
            autoHalo.gameObject.SetActive(autoCombat);
            Heroes();
            Buttons(false);
        }

        public void Hide()
        {
            root.gameObject.SetActive(false);
            spellbook?.Close();
            info?.gameObject.SetActive(false);
            tip?.Hide();
            banner?.Hide();
            // A scenario left while the results were up (for the title, or a room that ended) leaves them behind.
            results?.Dismiss();
            question?.Close();
            Casting = SpellId.None;
            if (back != null)
            {
                manager.RemoveBackHandler(back);
            }
            if (manager.Art != null)
            {
                manager.Art.UseCursor(CursorKind.Default);
            }
            cursorNow = (CursorKind)(-1);
        }

        public void Finish(BattleResult result, BattleState battle)
        {
            Hide();
        }

        public void SetRound(int number)
        {
            round.text = $"Round {Mathf.Max(1, number)}";
        }

        /// <summary>The stack in hand is this device's: work out where it may go and what it may strike, and paint it.</summary>
        public void Show(BattleState battle)
        {
            root.gameObject.SetActive(true);
            BattleStack stack = battle.Current;
            if (stack == null)
            {
                return;
            }
            if (Stale(stack))
            {
                Compute(stack);
            }
            Paint(stack);
            Buttons(true);
            Heroes();
        }

        /// <summary>How much of the height of the screen the bar covers, from the bottom (0 to 1).</summary>
        public float CoveredBottom()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
            return Mathf.Clamp01((PanelHeight + BandHeight) * scale / Mathf.Max(1f, Screen.height));
        }

        /// <summary>The part of the screen the field is framed in, above the bar, in viewport units.</summary>
        public Rect FreeArea()
        {
            float bottom = CoveredBottom();
            return new Rect(0.02f, bottom + 0.02f, 0.96f, 0.96f - bottom - 0.02f);
        }

        /// <summary>How black the screen is, from the fade into and out of a battlefield (0: not at all).</summary>
        public float Blackout => fade != null && fade.gameObject.activeSelf ? fade.Alpha : 0f;

        public IEnumerator Fade(float alpha, float seconds)
        {
            yield return fade.To(alpha, seconds);
        }

        public void ClearFade()
        {
            fade.Clear();
        }

        /// <summary>The call to arms: who fights whom, across the top of the field.</summary>
        public void Intro(BattleState start, GameState state)
        {
            banner.Show(start.town >= 0 ? "Siege!" : "To Battle!", $"{SideName(start, 0, state)}  against  {SideName(start, 1, state)}");
        }

        public void ShowResults(GameEvent ended, BattleState end, int side, GameState state, Action closed)
        {
            spellbook.Close();
            info.gameObject.SetActive(false);
            tip.Hide();
            View?.ClearHighlight();
            Buttons(false);
            // The battle is over: nobody is in hand or waits a turn any more.
            current.gameObject.SetActive(false);
            foreach (BattleQueueChip chip in chips)
            {
                chip.gameObject.SetActive(false);
            }
            nextRoundMark.gameObject.SetActive(false);
            status.text = "";
            results.Show(ended, end, side, state, closed);
        }

        private static string SideName(BattleState battle, int side, GameState state)
        {
            HeroState hero = state.Hero(battle.HeroOf(side));
            if (hero != null)
            {
                return hero.Name;
            }
            TownState town = side == 1 ? state.Town(battle.town) : null;
            if (town != null)
            {
                return town.name;
            }
            foreach (BattleStack stack in battle.stacks)
            {
                if (stack.side == side && !stack.IsTower)
                {
                    return $"the {stack.Def.Plural}";
                }
            }
            return "the wilds";
        }

        // ------------------------------------------------------------------ keeping the bar up to date

        private void Update()
        {
            if (!root.gameObject.activeSelf || Game == null || Shown == null || results.IsOpen)
            {
                return;
            }
            Refresh();
            Clock();
            bool ours = OurTurn();
            if (!ours)
            {
                Buttons(false);
            }
            if (spellbook.IsOpen && (!Game.InBattle || Game.Battle.current != bookFor))
            {
                // The turn went on without the player (the clock of an online game moved for them).
                spellbook.Close();
            }
            if (question.IsOpen && (!Game.InBattle || Game.Battle.current != askedFor))
            {
                // So did the question of a retreat, asked for a stack that is no longer in hand.
                question.Close();
            }
            Keys(ours);
            if (autoCombat && ours && Casting == SpellId.None && !spellbook.IsOpen && !question.IsOpen && Time.unscaledTime >= autoNext)
            {
                autoNext = Time.unscaledTime + 0.25f;
                Send(BattleAI.Next(Game));
            }
        }

        /// <summary>
        /// Online with a turn clock, the seconds left to whoever the battle waits for, before the round's number (which
        /// makes room for it); red for the last ten. As on the adventure map's bar, which has the clock of the map.
        /// </summary>
        private void Clock()
        {
            ServerClient server = manager.IsOnlineGame && manager.Online != null ? manager.Online.ServerClient : null;
            RoomTurnInfo timer = server != null ? server.Turn : null;
            bool timed = timer != null && timer.HasLimit;
            if (clock.gameObject.activeSelf != timed)
            {
                clock.gameObject.SetActive(timed);
                var number = (RectTransform)round.transform;
                number.sizeDelta = new Vector2(timed ? 230f - ClockWidth : 230f, number.sizeDelta.y);
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

        /// <summary>Whether this device may give the stack in hand its orders now.</summary>
        private bool OurTurn()
        {
            return manager.WaitingForHuman && !manager.HeroesUI.Busy && Game.InBattle && Game.State.pending.Count == 0 &&
                   Game.Battle.Current != null && !results.IsOpen && manager.IsGameRunning;
        }

        /// <summary>The card of the stack in hand, the order of the rest and the heroes, redrawn only when they change.</summary>
        private void Refresh()
        {
            BattleState shown = Shown;
            if (shown == null)
            {
                return;
            }
            BattleStack stack = shown.Current;
            // Asked every frame, so told without making anything: what the card shows of the stack, as numbers.
            (int, int, int, int, int) key = stack != null ? (stack.id, stack.count, stack.health, stack.shots, SpellsOn(stack)) : (-1, 0, 0, 0, 0);
            if (!cardDrawn || key != cardKey)
            {
                cardKey = key;
                cardDrawn = true;
                current.gameObject.SetActive(stack != null && stack.alive);
                if (stack != null && stack.alive)
                {
                    current.Show(Game, shown, stack, View.SideColor(stack.side));
                }
                Heroes();
            }
            Queue();
            status.text = OurTurn() ? "Your move" : stack == null ? "" : Local(shown.PlayerOf(stack.side)) ? "Your troops act" :
                shown.PlayerOf(stack.side) < 0 ? "The wilds move" : "The enemy moves";
        }

        private void Heroes()
        {
            BattleState shown = Shown;
            if (shown == null || Game == null)
            {
                return;
            }
            for (int side = 0; side < 2; side++)
            {
                heroes[side].Show(Game, shown, side, View.SideColor(side));
            }
        }

        /// <summary>
        /// The stack acting on the screen (lit), the stacks still to act this round in their order, those that waited
        /// after them, and the next round's order after a mark. The order comes from the rules when they have not run
        /// ahead of the view by a battle; the counts always come from the view's copy of the battle.
        /// </summary>
        private void Queue()
        {
            BattleState shown = Shown;
            BattleState live = Game.InBattle && !Game.HasEvents && SameBattle(Game.Battle, shown) ? Game.Battle : null;
            if (live == null)
            {
                return;
            }
            now.Clear();
            waiting.Clear();
            later.Clear();
            // The stack on the screen acts first, even while the rules have gone on to the next (its move still plays).
            BattleStack acting = Visible(shown, shown.current);
            if (acting != null)
            {
                now.Add(acting);
            }
            foreach (int id in live.order)
            {
                BattleStack stack = Visible(shown, id);
                // Those who waited act after the rest, as the rules take them: they come after, not here too.
                if (stack != null && stack != acting && !IsDone(live, id) && !live.Stack(id).waited)
                {
                    now.Add(stack);
                }
            }
            foreach (int id in live.waiting)
            {
                BattleStack stack = Visible(shown, id);
                if (stack != null && stack != acting && !IsDone(live, id))
                {
                    waiting.Add(stack);
                }
            }
            // Asked every frame while the player thinks: sorted and compared without making anything new.
            sorting = shown;
            slowestFirst = slowestFirst ?? SlowestFirst;
            fastestFirst = fastestFirst ?? FastestFirst;
            waiting.Sort(slowestFirst);
            now.AddRange(waiting);
            foreach (BattleStack stack in shown.stacks)
            {
                if (stack.alive)
                {
                    later.Add(stack);
                }
            }
            later.Sort(fastestFirst);
            sorting = null;

            // Which stack is lit is part of what is drawn: the same order with another stack acting is drawn again.
            queueSeen.Clear();
            queueSeen.Add(acting != null ? acting.id : -1);
            foreach (BattleStack stack in now)
            {
                queueSeen.Add(stack.id);
                queueSeen.Add(stack.count);
            }
            queueSeen.Add(-1);
            foreach (BattleStack stack in later)
            {
                queueSeen.Add(stack.id);
                queueSeen.Add(stack.count);
            }
            if (!queueEmpty && Same(queueSeen, queueDrawn))
            {
                return;
            }
            queueEmpty = false;
            (queueDrawn, queueSeen) = (queueSeen, queueDrawn);
            int used = 0;
            foreach (BattleStack stack in now)
            {
                if (used >= chips.Count)
                {
                    break;
                }
                chips[used].gameObject.SetActive(true);
                chips[used].transform.SetSiblingIndex(used);
                chips[used].Show(stack, View.SideColor(stack.side), false, used == 0 && stack == acting);
                used++;
            }
            nextRoundMark.SetSiblingIndex(used);
            nextRoundMark.gameObject.SetActive(used < chips.Count);
            foreach (BattleStack stack in later)
            {
                if (used >= chips.Count)
                {
                    break;
                }
                chips[used].gameObject.SetActive(true);
                chips[used].transform.SetSiblingIndex(used + 1);
                chips[used++].Show(stack, View.SideColor(stack.side), true, false);
            }
            for (int i = used; i < chips.Count; i++)
            {
                chips[i].gameObject.SetActive(false);
            }
        }

        /// <summary>Those who waited act slowest first, as the rules take them.</summary>
        private int SlowestFirst(BattleStack a, BattleStack b)
        {
            return BattleNumbers.Speed(Game, sorting, a).CompareTo(BattleNumbers.Speed(Game, sorting, b));
        }

        /// <summary>The next round's order: fastest first, the attacker's before the defender's at the same speed.</summary>
        private int FastestFirst(BattleStack a, BattleStack b)
        {
            int sa = a.IsTower ? 10 : BattleNumbers.Speed(Game, sorting, a);
            int sb = b.IsTower ? 10 : BattleNumbers.Speed(Game, sorting, b);
            return sa != sb ? sb.CompareTo(sa) : a.side != b.side ? a.side.CompareTo(b.side) : a.id.CompareTo(b.id);
        }

        private static bool Same(List<int> a, List<int> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>The spells on a stack and the rounds each has left, as one number: the card shows them all.</summary>
        private static int SpellsOn(BattleStack stack)
        {
            int sum = stack.effects.Count;
            foreach (EffectState effect in stack.effects)
            {
                sum = sum * 31 + effect.spell * 17 + effect.rounds;
            }
            return sum;
        }

        /// <summary>Whether a player (a seat) is led from this device: online the seat here, at one device any person.</summary>
        private bool Local(int player)
        {
            if (player < 0)
            {
                return false;
            }
            if (manager.IsOnlineGame)
            {
                return player == manager.LocalSeat;
            }
            PlayerState state = Game.State.Player(player);
            return state != null && state.human;
        }

        private static BattleStack Visible(BattleState shown, int id)
        {
            BattleStack stack = shown.Stack(id);
            return stack != null && stack.alive ? stack : null;
        }

        private static bool IsDone(BattleState live, int id)
        {
            BattleStack stack = live.Stack(id);
            return stack == null || !stack.alive || stack.acted;
        }

        private static bool SameBattle(BattleState a, BattleState b)
        {
            return a != null && b != null && a.mapCell == b.mapCell && a.attackerHero == b.attackerHero &&
                   a.fieldSeed == b.fieldSeed && a.monsterObject == b.monsterObject && a.town == b.town;
        }

        private void Buttons(bool ours)
        {
            BattleState battle = ours ? Game.Battle : Shown;
            BattleStack stack = battle != null ? battle.Current : null;
            HeroState hero = stack != null ? Game.State.Hero(battle.HeroOf(stack.side)) : null;
            bool mine = ours && stack != null;
            UIKit.Enable(wait, mine && !stack.waited && !stack.moraleUsed);
            UIKit.Enable(defend, mine);
            UIKit.Enable(cast, mine && hero != null && hero.spells.Count > 0 && !battle.HasCast(stack.side));
            UIKit.Enable(retreat, mine && hero != null && !(stack.side == 1 && battle.town >= 0));
            UIKit.Enable(options, true);
            UIKit.Enable(auto, true);
        }

        // ------------------------------------------------------------------ the combat log

        /// <summary>A line of the combat log, in the colors of the side it is about.</summary>
        public void Log(string text, int side)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            Color color = side >= 0 && View != null ? Color.Lerp(UIKit.Ink, HeroesArt.PlayerColor(View.SideColor(side)), 0.32f) : UIKit.Dim;
            log.Add((text, color));
            if (log.Count > 40)
            {
                log.RemoveAt(0);
            }
            DrawLog();
        }

        /// <summary>Shows the last lines of the combat log (none left from the battle before, once it is cleared).</summary>
        private void DrawLog()
        {
            for (int i = 0; i < LogLines; i++)
            {
                int index = log.Count - LogLines + i;
                bool on = index >= 0;
                logLines[i].text = on ? log[index].text : "";
                Color shade = on ? log[index].color : UIKit.Dim;
                // The older lines fade back.
                shade.a = i == LogLines - 1 ? 1f : i == LogLines - 2 ? 0.82f : 0.64f;
                logLines[i].color = shade;
            }
        }

        // ------------------------------------------------------------------ the pointer on the field

        /// <summary>
        /// Reads the pointer over the field (the interface calls this every frame of a battle): the ring and the pointer
        /// for what a click would do, the tooltip of a blow, the card of a stack under a held right button, and the click.
        /// </summary>
        public void Pointer()
        {
            IBattlefield field = manager.Field;
            if (field == null || Game == null || Shown == null || !root.gameObject.activeSelf || results.IsOpen)
            {
                tip.Hide();
                return;
            }
            bool overUi = TourPointer == null && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            Vector3 ground = default;
            int cell = -1;
            if (!overUi && !spellbook.IsOpen)
            {
                // A creature is what the pointer is on wherever on its body, before the ground behind its head.
                cell = View.StackUnder(PointerAt, out ground);
                if (cell < 0)
                {
                    cell = field.CellAt(PointerAt, out ground);
                }
            }
            tip.At = PointerAt;
            bool click = TourClick || Input.GetMouseButtonDown(0);
            TourClick = false;
            Info(cell);
            bool ours = OurTurn();
            if (!ours)
            {
                View.Hover(-1, Color.clear);
                tip.Hide();
                Cursor(overUi ? CursorKind.Default : CursorKind.Wait);
                return;
            }
            BattleState battle = Game.Battle;
            BattleStack stack = battle.Current;
            if (Stale(stack))
            {
                Compute(stack);
                Paint(stack);
            }
            Act act = Resolve(battle, stack, cell, ground, out int from, out BattleStack target);
            HoverFeedback(field, battle, stack, cell, act, from, target);
            if (cell >= 0 && click && !field.IsDragging && !autoCombat)
            {
                Click(act, stack, cell, from, target);
            }
        }

        /// <summary>The card of a stack while the right button is held on it (or the spell aimed is let go of).</summary>
        private void Info(int cell)
        {
            bool held = TourHold || Input.GetMouseButton(1);
            if (Input.GetMouseButtonDown(1) || TourHold && infoStack < 0)
            {
                if (Casting != SpellId.None)
                {
                    CancelCast();
                    return;
                }
                BattleStack stack = cell >= 0 ? Shown.StackAt(cell) : null;
                if (stack != null)
                {
                    infoStack = stack.id;
                    info.Show(Game, Shown, stack, View.SideColor(stack.side));
                    info.gameObject.SetActive(true);
                    info.transform.SetAsLastSibling();
                }
            }
            if (infoStack >= 0 && !held)
            {
                infoStack = -1;
                info.gameObject.SetActive(false);
            }
        }

        /// <summary>What a click on <paramref name="cell"/> would do for the stack in hand.</summary>
        private Act Resolve(BattleState battle, BattleStack stack, int cell, Vector3 ground, out int from, out BattleStack target)
        {
            from = -1;
            target = null;
            if (cell < 0)
            {
                return Act.Nothing;
            }
            if (Casting != SpellId.None)
            {
                target = battle.StackAt(cell);
                return castable.Contains(cell) ? Act.Cast : Act.Blocked;
            }
            BattleStack there = battle.StackAt(cell);
            if (there != null && there.side != stack.side)
            {
                target = there;
                if (Game.CanShoot(stack))
                {
                    return Act.Shoot;
                }
                List<int> cells = StrikeCells(stack, there);
                if (cells.Count == 0)
                {
                    return Act.Blocked;
                }
                from = Nearest(cells, there, ground);
                return Act.Strike;
            }
            if (there != null)
            {
                return Act.Nothing;
            }
            if (reach.ContainsKey(cell) && cell != stack.cell)
            {
                return stack.Def.IsFlying ? Act.Fly : Act.Move;
            }
            return Act.Blocked;
        }

        /// <summary>
        /// Of the cells a blow could be struck from, the one on the side of the target the pointer is on: the pointer's
        /// place in the target's hexagon picks the neighbour it strikes from, as in the old games.
        /// </summary>
        private int Nearest(List<int> cells, BattleStack target, Vector3 ground)
        {
            if (cells.Count == 1)
            {
                return cells[0];
            }
            IBattlefield field = manager.Field;
            Vector3 center = field.Point(target.cell);
            Vector3 toward = ground - center;
            toward.y = 0f;
            int best = cells[0];
            float closest = float.MaxValue;
            foreach (int cell in cells)
            {
                Vector3 side = field.Point(cell) - center;
                side.y = 0f;
                // The direction matters more than the distance: the neighbour the pointer leans toward.
                float score = toward.sqrMagnitude < 0.01f ? side.sqrMagnitude : -Vector3.Dot(side.normalized, toward.normalized);
                if (score < closest)
                {
                    closest = score;
                    best = cell;
                }
            }
            return best;
        }

        private void HoverFeedback(IBattlefield field, BattleState battle, BattleStack stack, int cell, Act act, int from, BattleStack target)
        {
            switch (act)
            {
                case Act.Move:
                    View.Hover(cell, BattleView.HoverColor);
                    Cursor(CursorKind.Move);
                    tip.Hide();
                    break;
                case Act.Fly:
                    View.Hover(cell, BattleView.HoverColor);
                    Cursor(CursorKind.Fly);
                    tip.Hide();
                    break;
                case Act.Strike:
                {
                    View.Hover(from, new Color(1f, 0.55f, 0.4f, 0.95f));
                    Camera view = field.Camera;
                    Vector3 a = view != null ? view.WorldToScreenPoint(field.Point(from)) : Vector3.zero;
                    Vector3 b = view != null ? view.WorldToScreenPoint(field.Point(target.cell)) : Vector3.right;
                    Cursor(HeroesArt.Strike(new Vector2(b.x - a.x, b.y - a.y)));
                    int steps = reach.TryGetValue(from, out int walked) ? walked : 0;
                    tip.Show(Tip(Act.Strike, stack, target, steps));
                    break;
                }
                case Act.Shoot:
                    View.Hover(cell, new Color(1f, 0.85f, 0.35f, 0.95f));
                    Cursor(CursorKind.Shoot);
                    tip.Show(Tip(Act.Shoot, stack, target, 0));
                    break;
                case Act.Cast:
                    View.Hover(cell, new Color(0.8f, 0.65f, 1f, 0.95f));
                    Cursor(CursorKind.Cast);
                    tip.Show(Tip(Act.Cast, stack, target, 0));
                    break;
                case Act.Blocked:
                    View.Hover(-1, Color.clear);
                    Cursor(CursorKind.Blocked);
                    tip.Hide();
                    break;
                default:
                    View.Hover(-1, Color.clear);
                    BattleStack there = cell >= 0 ? battle.StackAt(cell) : null;
                    Cursor(there != null ? CursorKind.Info : CursorKind.Default);
                    tip.Hide();
                    break;
            }
        }

        /// <summary>
        /// Whether the cells worked out for the stack in hand are out of date: another stack, another spell aimed, or the
        /// same stack again after anything at all was done on the field (it waited or defended, and the others moved).
        /// </summary>
        private bool Stale(BattleStack stack)
        {
            return computedFor != stack.id || computedSpell != Casting || computedAt != Game.Battle.actions;
        }

        /// <summary>The cells the stack in hand could strike <paramref name="target"/> from, worked out once while it hovers.</summary>
        private List<int> StrikeCells(BattleStack stack, BattleStack target)
        {
            if (strikeFrom == null || strikeReach != reach || strikeBy != stack.id || strikeAt != target.id)
            {
                strikeFrom = Game.AttackCells(stack, target, reach);
                strikeReach = reach;
                strikeBy = stack.id;
                strikeAt = target.id;
            }
            return strikeFrom;
        }

        /// <summary>The tooltip of a blow, a shot or a spell under the pointer, written once while it stays on the same thing.</summary>
        private string Tip(Act act, BattleStack stack, BattleStack target, int steps)
        {
            (int, int, int, int, int) key = ((int)act, stack.id, target != null ? target.id : -1, steps, (int)Casting);
            if (tipText != null && tipReach == reach && key == tipKey)
            {
                return tipText;
            }
            tipKey = key;
            tipReach = reach;
            tipText = act == Act.Cast ? SpellEstimate(stack, target) : Estimate(act == Act.Shoot ? "Shoot" : "Attack", stack, target, act == Act.Shoot, steps);
            return tipText;
        }

        private string Estimate(string verb, BattleStack stack, BattleStack target, bool ranged, int steps)
        {
            Game.DamageEstimate(stack, target, ranged, steps, out int least, out int most, out int fewest, out int mostKills);
            string damage = least == most ? least.ToString() : $"{least}-{most}";
            string kills = fewest == mostKills ? fewest.ToString() : $"{fewest}-{mostKills}";
            string note = ranged && Game.BattleGrid.Distance(stack.cell, target.cell) > 10 && !stack.IsTower ? "\n<color=#C8B890><i>Far away: half damage</i></color>" : "";
            return $"<b>{verb} {BattleNumbers.Troop(target)}</b>\n<sprite name=\"damage\"> Damage {damage}   <sprite name=\"health\"> Kills {kills}{note}";
        }

        private string SpellEstimate(BattleStack stack, BattleStack target)
        {
            SpellDef def = Spells.Get(Casting);
            if (def == null)
            {
                return null;
            }
            HeroState hero = Game.State.Hero(Game.Battle.HeroOf(stack.side));
            if (def.IsDamage && hero != null && target != null)
            {
                int damage = Game.SpellDamage(hero, def, target);
                int kills = Game.Kills(target, damage);
                return $"<b>{def.Name}: {BattleNumbers.Troop(target)}</b>\n<sprite name=\"damage\"> Damage {damage}   <sprite name=\"health\"> Kills {kills}";
            }
            return target != null ? $"<b>{def.Name}: {BattleNumbers.Troop(target)}</b>" : $"<b>{def.Name}</b>";
        }

        private void Cursor(CursorKind kind)
        {
            if (manager.Art != null && cursorNow != kind)
            {
                cursorNow = kind;
                manager.Art.UseCursor(kind);
            }
        }

        private CursorKind cursorNow = (CursorKind)(-1);

        /// <summary>The pointer the field shows now (for the development tours, which cannot see the hardware cursor).</summary>
        public CursorKind Pointing => cursorNow;

        /// <summary>
        /// The interface put the plain pointer up over the battle (the pause menu): the field sets its own again the next
        /// time it reads the pointer.
        /// </summary>
        public void ForgetPointer()
        {
            cursorNow = (CursorKind)(-1);
        }

        private void Click(Act act, BattleStack stack, int cell, int from, BattleStack target)
        {
            IHeroesCommands commands = manager.Commands;
            switch (act)
            {
                case Act.Move:
                case Act.Fly:
                    commands.BattleMove(stack.id, cell);
                    break;
                case Act.Strike:
                    commands.BattleAttack(stack.id, target.id, from);
                    break;
                case Act.Shoot:
                    commands.BattleShoot(stack.id, target.id);
                    break;
                case Act.Cast:
                    SpellId spell = Casting;
                    Casting = SpellId.None;
                    commands.BattleCast(spell, cell);
                    break;
                case Act.Blocked:
                    manager.Sound?.Play(Sfx.Error, 0.5f);
                    return;
                default:
                    return;
            }
            tip.Hide();
            View.Hover(-1, Color.clear);
            View.ClearHighlight();
            computedFor = -1;
        }

        /// <summary>The cells the stack can step onto, the enemies it can strike, the ones it can shoot, and a spell's targets.</summary>
        private void Compute(BattleStack stack)
        {
            if (Casting != SpellId.None && Game.CannotCast(stack.side, Casting) != null)
            {
                // A spell picked for a turn that went by without it can no longer be cast now.
                Casting = SpellId.None;
            }
            computedFor = stack.id;
            computedSpell = Casting;
            computedAt = Game.Battle.actions;
            BattleState battle = Game.Battle;
            reach = Game.BattleReach(stack);
            targets.Clear();
            shots.Clear();
            castable.Clear();
            foreach (BattleStack other in battle.stacks)
            {
                if (!other.alive || other.side == stack.side)
                {
                    continue;
                }
                shots.Add(other.cell);
                if (Game.AttackCells(stack, other, reach).Count > 0)
                {
                    targets.Add(other.cell);
                }
            }
            if (Casting != SpellId.None)
            {
                foreach (int cell in battle.cells)
                {
                    if (Game.ValidSpellTarget(stack.side, Casting, cell))
                    {
                        castable.Add(cell);
                    }
                }
            }
        }

        private void Paint(BattleStack stack)
        {
            if (Casting != SpellId.None)
            {
                View.Highlight(null, castable, BattleView.SpellColor, stack.cell);
                return;
            }
            // A shooter with an enemy next to it cannot shoot: it strikes the ones it can reach instead.
            bool shooting = Game.CanShoot(stack);
            var moves = new List<int>(reach.Keys);
            moves.Remove(stack.cell);
            View.Highlight(moves, shooting ? shots : targets, shooting ? BattleView.ShootColor : BattleView.StrikeColor, stack.cell);
        }

        // ------------------------------------------------------------------ the commands

        private void Keys(bool ours)
        {
            if (MenuOver() || results.IsOpen || question.IsOpen)
            {
                return;
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                ToggleAuto();
            }
            if (!ours || spellbook.IsOpen)
            {
                return;
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                Retreat();
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                OpenSpells();
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                Wait();
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.Space))
            {
                Defend();
            }
        }

        /// <summary>
        /// Escape takes back the question of a retreat, closes the book, lets go of a spell being aimed, before it opens
        /// the menu. A menu already over the battle (the pause menu, online the match menu, which the game goes on under)
        /// is what Escape closes first: nothing behind it changes.
        /// </summary>
        private bool Back()
        {
            if (MenuOver())
            {
                return false;
            }
            if (question.IsOpen)
            {
                question.Close();
                return true;
            }
            if (spellbook.IsOpen)
            {
                spellbook.Close();
                return true;
            }
            if (Casting != SpellId.None)
            {
                CancelCast();
                return true;
            }
            return false;
        }

        private void CancelCast()
        {
            Casting = SpellId.None;
            computedFor = -1;
            if (OurTurn())
            {
                Show(Game.Battle);
            }
        }

        private void OpenMenu()
        {
            manager.OpenMenu();
        }

        private void Wait()
        {
            if (OurTurn())
            {
                manager.Commands.BattleWait(Game.Battle.current);
            }
        }

        private void Defend()
        {
            if (OurTurn())
            {
                manager.Commands.BattleDefend(Game.Battle.current);
            }
        }

        /// <summary>
        /// Whether a menu lies over the battle: the pause menu at one device (the game stops under it), or online the
        /// match menu or the lobby of the room (the game goes on under them). The keys of the bar and Escape are the
        /// menu's while it is up.
        /// </summary>
        private bool MenuOver()
        {
            if (!manager.IsGameRunning)
            {
                return true;
            }
            OnlineLobbyUI lobby = manager.Online != null ? manager.Online.LobbyUI : null;
            return lobby != null && (lobby.IsMatchMenuOpen || lobby.IsOpen);
        }

        /// <summary>
        /// A retreat asks first, as in the old games: the hero flees the field and his whole army is lost, which a key
        /// pressed by mistake (R sits by the keys of the other commands) must not do. R again or Enter takes it, and only
        /// while the same stack is still in hand.
        /// </summary>
        public void Retreat()
        {
            if (!OurTurn() || question.IsOpen)
            {
                return;
            }
            BattleState battle = Game.Battle;
            int side = battle.Current.side;
            HeroState hero = Game.State.Hero(battle.HeroOf(side));
            if (hero == null || side == 1 && battle.town >= 0)
            {
                // No hero to lead the flight, or the defender of a town, who holds it to the last (the button is off too).
                manager.Sound?.Play(Sfx.Error, 0.5f);
                return;
            }
            bool she = hero.Def != null && hero.Def.Female;
            askedFor = battle.current;
            tip.Hide();
            question.Ask("Retreat?",
                $"{hero.Name} will flee the field, and every creature in {(she ? "her" : "his")} army will be lost. " +
                $"{(she ? "She" : "He")} keeps {(she ? "her" : "his")} artifacts, and may be hired again.",
                "Retreat", KeyCode.R, () =>
                {
                    if (OurTurn() && Game.Battle.current == askedFor)
                    {
                        manager.Commands.BattleRetreat();
                    }
                });
        }

        /// <summary>Turns auto combat on or off (the A key and the button toggle it).</summary>
        public void SetAuto(bool on)
        {
            if (on != autoCombat)
            {
                ToggleAuto();
            }
        }

        private void ToggleAuto()
        {
            autoCombat = !autoCombat;
            autoHalo.gameObject.SetActive(autoCombat);
            autoNext = Time.unscaledTime + 0.3f;
            Log(autoCombat ? "Auto combat: the computer leads your troops." : "Auto combat is off.", -1);
            if (autoCombat)
            {
                Casting = SpellId.None;
                spellbook.Close();
            }
        }

        /// <summary>Opens the spellbook of the hero whose stack is in hand, when it is this device's move (the S key).</summary>
        public void OpenSpells()
        {
            if (!OurTurn())
            {
                return;
            }
            BattleState battle = Game.Battle;
            BattleStack stack = battle.Current;
            HeroState hero = Game.State.Hero(battle.HeroOf(stack.side));
            if (hero != null)
            {
                tip.Hide();
                bookFor = stack.id;
                spellbook.Show(hero, stack.side, Pick);
            }
        }

        /// <summary>A spell was picked from the book: its targets are painted and the next click on one casts it.</summary>
        public void Pick(SpellId spell)
        {
            spellbook.Close();
            Casting = spell;
            computedFor = -1;
            SpellDef def = Spells.Get(spell);
            Log(def != null ? $"{def.Name}: choose a target, or right click to put the book away." : "", -1);
            if (OurTurn())
            {
                Show(Game.Battle);
            }
        }

        /// <summary>
        /// A move of the computer's battle sense, made as the player's own command. The computer would retreat from a
        /// battle it thinks lost; for a player that is the player's own call, so auto combat stops and leaves it to them.
        /// </summary>
        private void Send(GameCommand move)
        {
            if (move == null)
            {
                return;
            }
            if (move.kind == CommandKind.BattleRetreat)
            {
                SetAuto(false);
                Log("Auto combat stops: the battle is going badly. Fight on, or retreat (R).", -1);
                return;
            }
            IHeroesCommands commands = manager.Commands;
            switch (move.kind)
            {
                case CommandKind.BattleMove:
                    commands.BattleMove(move.a, move.b);
                    break;
                case CommandKind.BattleAttack:
                    commands.BattleAttack(move.a, move.b, move.c);
                    break;
                case CommandKind.BattleShoot:
                    commands.BattleShoot(move.a, move.b);
                    break;
                case CommandKind.BattleWait:
                    commands.BattleWait(move.a);
                    break;
                case CommandKind.BattleCast:
                    commands.BattleCast((SpellId)move.a, move.b);
                    break;
                case CommandKind.BattleRetreat:
                    commands.BattleRetreat();
                    break;
                default:
                    commands.BattleDefend(move.a);
                    break;
            }
            View.ClearHighlight();
            computedFor = -1;
        }
    }
}
