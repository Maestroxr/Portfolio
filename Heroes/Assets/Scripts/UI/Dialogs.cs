using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// A window over the map: the map darkens behind it, the window grows in, its title is written on a crimson ribbon
    /// across its top edge, its body is the leather inside the frame and its answers are a row of buttons along the
    /// bottom.
    /// </summary>
    public class Dialog : MonoBehaviour
    {
        /// <summary>The height the row of answers takes at the bottom of a window.</summary>
        protected const float ButtonRow = 58f;

        /// <summary>How far the ribbon of the title rises above the top edge of its window.</summary>
        protected const float RibbonRise = 30f;

        /// <summary>
        /// The tallest a window kept under the bar along the top can be (<see cref="KeepUnderTopBar"/>): the height of the
        /// layout, less the bar, the ribbon over the window and a margin above and below.
        /// </summary>
        protected const float UnderBarHeight = 1080f - UnderBarTop - UnderBarBottom;

        private const float UnderBarTop = AdventureHud.Margin + AdventureHud.TopHeight + RibbonRise + 4f;
        private const float UnderBarBottom = 10f;

        protected HeroesGameManager Manager;
        protected RectTransform Window;
        protected RectTransform Body;
        protected RectTransform Buttons;
        protected TextMeshProUGUI Heading;
        private RectTransform ribbon;
        private RectTransform shade;
        private CanvasGroup group;
        private string titled;
        private Coroutine opening;
        private bool underBar;

        /// <summary>Builds the empty window, the size asked for, in the middle of the screen.</summary>
        protected static T Build<T>(Transform parent, HeroesGameManager manager, string name, Vector2 size, string title)
            where T : Dialog
        {
            RectTransform holder = UIKit.Rect(parent, name);
            UIKit.Stretch(holder);
            var dialog = holder.gameObject.AddComponent<T>();
            dialog.Manager = manager;
            dialog.group = holder.gameObject.AddComponent<CanvasGroup>();

            Image shade = UIKit.Shade(holder, "Shade", 0.82f);
            shade.raycastTarget = true;
            dialog.shade = (RectTransform)shade.transform;
            dialog.ShadeTop(false);

            Image frame = UIKit.Frame(holder, "Window");
            dialog.Window = (RectTransform)frame.transform;
            UIKit.Pin(dialog.Window, new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), size);
            dialog.Window.pivot = new Vector2(0.5f, 0.5f);
            Vector4 inset = UIKit.Inset(frame);

            Image band = UIKit.Ribbon(dialog.Window, "Ribbon", title, 30f);
            dialog.ribbon = UIKit.Pin((RectTransform)band.transform, new Vector2(0.5f, 1f), new Vector2(0f, RibbonRise), new Vector2(420f, 84f));
            dialog.ribbon.pivot = new Vector2(0.5f, 1f);
            dialog.Heading = band.GetComponentInChildren<TextMeshProUGUI>();
            UIKit.FitLine(dialog.Heading, 30f, 18f);

            dialog.Body = UIKit.Rect(dialog.Window, "Body");
            UIKit.Stretch(dialog.Body, inset.x + 18f, inset.y + ButtonRow + 22f, inset.z + 18f, inset.w + 52f);

            dialog.Buttons = UIKit.Rect(dialog.Window, "Buttons");
            dialog.Buttons.anchorMin = new Vector2(0f, 0f);
            dialog.Buttons.anchorMax = new Vector2(1f, 0f);
            dialog.Buttons.pivot = new Vector2(0.5f, 0f);
            dialog.Buttons.offsetMin = new Vector2(inset.x + 18f, inset.y + 12f);
            dialog.Buttons.offsetMax = new Vector2(-inset.z - 18f, inset.y + 12f + ButtonRow);
            HorizontalLayoutGroup layout = UIKit.Layout<HorizontalLayoutGroup>(dialog.Buttons, 16f);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;

            dialog.SizeRibbon();
            holder.gameObject.SetActive(false);
            return dialog;
        }

        public bool IsOpen => gameObject.activeSelf;

        public virtual void Close()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// For the screens of the map as big as the screen (a town, a hero; at most <see cref="UnderBarHeight"/> tall):
        /// the window and its ribbon keep under the bar along the top, which stays in sight and out of the shade while
        /// the panels of the map show, so the treasury can be read while it is spent.
        /// </summary>
        protected void KeepUnderTopBar()
        {
            underBar = true;
            // In the middle of the room left under the bar, whatever the height of the window.
            Window.anchoredPosition = new Vector2(0f, (UnderBarBottom - UnderBarTop) * 0.5f);
        }

        /// <summary>The shade reaches past the safe area to the edges of the screen, or stops at the bar along the top.</summary>
        private void ShadeTop(bool belowBar)
        {
            UIKit.Stretch(shade, -400f, -400f, -400f, belowBar ? AdventureHud.TopInset : -400f);
        }

        /// <summary>Opens the window over everything else, fading and growing in.</summary>
        protected void Open()
        {
            bool was = gameObject.activeSelf;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (underBar)
            {
                AdventureHud hud = Manager.HeroesUI != null ? Manager.HeroesUI.Hud : null;
                ShadeTop(hud != null && hud.IsShown);
            }
            SizeRibbon();
            if (!was)
            {
                if (opening != null)
                {
                    StopCoroutine(opening);
                }
                opening = StartCoroutine(Opening());
            }
        }

        private IEnumerator Opening()
        {
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.MoveTowards(t, 1f, Time.unscaledDeltaTime / 0.2f);
                float eased = Gamebox.Tween.OutCubic(t);
                group.alpha = eased;
                Window.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, eased);
                yield return null;
            }
            group.alpha = 1f;
            Window.localScale = Vector3.one;
            opening = null;
        }

        private void OnDisable()
        {
            if (group != null)
            {
                group.alpha = 1f;
            }
            if (Window != null)
            {
                Window.localScale = Vector3.one;
            }
            opening = null;
        }

        /// <summary>The ribbon grows with its title, within the window.</summary>
        private void SizeRibbon()
        {
            if (Heading == null || ribbon == null || titled == Heading.text)
            {
                return;
            }
            titled = Heading.text;
            float words = Heading.GetPreferredValues(Heading.text, 2000f, 60f).x;
            float wanted = Mathf.Clamp(words + 150f, 360f, Window.rect.width - 60f);
            ribbon.sizeDelta = new Vector2(wanted, ribbon.sizeDelta.y);
        }

        protected virtual void LateUpdate()
        {
            SizeRibbon();
        }

        protected void ClearBody()
        {
            UIKit.Clear(Body);
        }

        protected void ClearButtons()
        {
            UIKit.Clear(Buttons);
        }

        /// <summary>A button in the row of answers, as wide as its words need.</summary>
        protected Button Answer(string text, System.Action onClick)
        {
            Button button = UIKit.Push(Buttons, text, text, onClick, 24f);
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            float width = Mathf.Clamp(label.GetPreferredValues(text, 2000f, 40f).x + 70f, 190f, 420f);
            UIKit.Fit((RectTransform)button.transform, width, ButtonRow - 4f);
            return button;
        }

        /// <summary>Whether a click may send a command now (the map has played out what happened).</summary>
        protected bool MayCommand => Manager.HeroesUI == null || Manager.HeroesUI.CanCommand;
    }

    /// <summary>
    /// The question the rules are waiting on: which skill a hero learns on a level up, whether to take the gold or the
    /// wisdom of a chest, what the masters of the arena teach. Each answer is a card with its picture. Nothing else in
    /// the game moves until one is picked.
    /// </summary>
    public sealed class ChoiceBox : Dialog
    {
        private TextMeshProUGUI note;
        private RectTransform cards;

        public static ChoiceBox Make(Transform parent, HeroesGameManager manager)
        {
            ChoiceBox box = Build<ChoiceBox>(parent, manager, "Choice", new Vector2(860f, 530f), "A Choice");
            box.note = UIKit.Label(box.Body, "Note", "", 25f, UIKit.Ink, TextAlignmentOptions.Top);
            UIKit.Pin((RectTransform)box.note.transform, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(760f, 70f));
            box.cards = UIKit.Rect(box.Body, "Cards");
            box.cards.anchorMin = new Vector2(0f, 0f);
            box.cards.anchorMax = new Vector2(1f, 1f);
            box.cards.offsetMin = new Vector2(0f, 0f);
            box.cards.offsetMax = new Vector2(0f, -78f);
            HorizontalLayoutGroup layout = UIKit.Layout<HorizontalLayoutGroup>(box.cards, 28f);
            layout.childAlignment = TextAnchor.MiddleCenter;
            return box;
        }

        public void Show(PendingChoice pending)
        {
            if (pending == null)
            {
                Close();
                return;
            }
            UIKit.Clear(cards);
            ClearButtons();
            HeroState hero = Manager.Game.State.Hero(pending.hero);
            switch (pending.kind)
            {
                case ChoiceKind.LevelUp:
                    LevelUp(pending, hero);
                    break;
                case ChoiceKind.Treasure:
                    Treasure(pending);
                    break;
                default:
                    Arena();
                    break;
            }
            if (cards.childCount == 0)
            {
                Answer("Onward", () => Pick(0));
            }
            else
            {
                TextMeshProUGUI hint = UIKit.Label(Buttons, "Hint", "Click a card to choose.", 20f, UIKit.Dim, TextAlignmentOptions.Center);
                hint.fontStyle = FontStyles.Italic;
                UIKit.Fit((RectTransform)hint.transform, 600f, ButtonRow - 4f);
            }
            Open();
        }

        private void LevelUp(PendingChoice pending, HeroState hero)
        {
            Heading.text = hero != null ? $"{hero.Name} reaches level {hero.level}" : "A New Level";
            string raised = pending.stat >= 0
                ? $"{UIKit.Glyph(StatSprite((PrimaryStat)pending.stat))} <color=#8CE07E>+1 {HeroData.StatName((PrimaryStat)pending.stat)}</color>.  "
                : "";
            note.text = $"{raised}Choose what else the hero has learned.";
            for (int i = 0; i < pending.options.Length; i++)
            {
                int skill = pending.options[i];
                if (skill < 0)
                {
                    continue;
                }
                SkillDef def = HeroData.Skill((SkillId)skill);
                int level = hero != null ? hero.SkillLevel((SkillId)skill) : 0;
                string now = level > 0 ? $"From {HeroData.SkillLevelName(level)}" : "A new skill";
                Card(i, Manager.Art.Skill((SkillId)skill), true, $"{HeroData.SkillLevelName(level + 1)} {def.Name}", now,
                    Mathf.Clamp(level, 0, 2) < def.Levels.Length ? def.Levels[Mathf.Clamp(level, 0, 2)] : def.Description);
            }
        }

        private void Treasure(PendingChoice pending)
        {
            Heading.text = "A Chest of Treasure";
            note.text = "Gold for the coffers, or the lessons of the road?";
            // The rules pay the first option out as gold and the second as experience.
            Card(0, Manager.Art.Resource(ResourceKind.Gold), false, $"{pending.options[0]:N0} Gold", "For the treasury", "Spent on buildings, creatures and heroes.");
            Card(1, Manager.Art.Icon("experience"), true, $"{pending.options[1]:N0} Experience", "For the hero", "Brings the next level closer.");
        }

        private void Arena()
        {
            Heading.text = "The Arena";
            note.text = "The masters of the arena offer their training.";
            Card(0, Manager.Art.statIcons.Length > 0 ? Manager.Art.statIcons[0] : null, false, "+2 Attack", "Strike harder", "Every creature of the hero's army hits harder.");
            Card(1, Manager.Art.statIcons.Length > 1 ? Manager.Art.statIcons[1] : null, false, "+2 Defense", "Stand firmer", "Every creature of the hero's army takes less.");
        }

        private static string StatSprite(PrimaryStat stat)
        {
            switch (stat)
            {
                case PrimaryStat.Attack: return "attack";
                case PrimaryStat.Defense: return "defense";
                case PrimaryStat.Power: return "power";
                default: return "knowledge";
            }
        }

        /// <summary>An answer as a card: its picture in a round frame, what it is, and a line on what it does.</summary>
        private void Card(int option, Sprite icon, bool tinted, string title, string detail, string text)
        {
            Button button = UIKit.CardButton(cards, title, () => Pick(option));
            UIKit.Fit((RectTransform)button.transform, 330f, 276f);
            RectTransform content = UIKit.Content((Image)button.targetGraphic, 10f);
            Image picture = UIKit.PortraitFrame(content, "Icon", icon, true);
            picture.preserveAspect = true;
            picture.color = tinted ? UIKit.Gold : Color.white;
            RectTransform frame = (RectTransform)picture.transform.parent.parent;
            UIKit.Pin(frame, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(110f, 110f));
            UIKit.Stretch((RectTransform)picture.transform, 18f, 18f, 18f, 18f);
            TextMeshProUGUI name = UIKit.Label(content, "Name", title, 25f, UIKit.Gold, TextAlignmentOptions.Center, true);
            UIKit.Pin((RectTransform)name.transform, new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(290f, 34f));
            UIKit.FitLine(name, 25f, 16f);
            UIKit.Look(name, TextLook.Gold);
            TextMeshProUGUI sub = UIKit.Label(content, "Detail", detail, 19f, UIKit.Dim, TextAlignmentOptions.Center);
            sub.fontStyle = FontStyles.Italic;
            UIKit.Pin((RectTransform)sub.transform, new Vector2(0.5f, 1f), new Vector2(0f, -152f), new Vector2(290f, 26f));
            UIKit.FitLine(sub, 19f, 14f);
            TextMeshProUGUI words = UIKit.Label(content, "Text", text, 19f, UIKit.Ink, TextAlignmentOptions.Top);
            RectTransform wordsRect = (RectTransform)words.transform;
            wordsRect.anchorMin = new Vector2(0f, 0f);
            wordsRect.anchorMax = new Vector2(1f, 1f);
            wordsRect.offsetMin = new Vector2(8f, 4f);
            wordsRect.offsetMax = new Vector2(-8f, -186f);
            words.enableAutoSizing = true;
            words.fontSizeMax = 19f;
            words.fontSizeMin = 14f;
        }

        private void Pick(int option)
        {
            if (!MayCommand)
            {
                return;
            }
            Manager.Commands.Choose(option);
            Close();
        }
    }

    /// <summary>
    /// What the scenario came to: won or lost, in how many days, and at one device how many stars of the chapter that is
    /// worth. Online it says how the realm of the player at this device fared and leads back to the room.
    /// </summary>
    public sealed class ResultsBox : Dialog
    {
        public static ResultsBox Make(Transform parent, HeroesGameManager manager)
        {
            return Build<ResultsBox>(parent, manager, "Results", new Vector2(860f, 600f), "");
        }

        public void Show(bool won, int stars, int days)
        {
            ClearBody();
            ClearButtons();
            Heading.text = won ? "Victory" : "Defeat";
            bool online = Manager.IsOnlineGame;
            HeroesLevel scenario = online ? null : Manager.Scenario;
            bool chapter = scenario != null && !scenario.IsSkirmish;

            TextMeshProUGUI verdict = UIKit.Heading(Body, "Verdict",
                won ? online ? "Your realm carries the day." : "The land is yours." : "Your banners have fallen.", 34f);
            UIKit.Pin((RectTransform)verdict.transform, new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(760f, 46f));
            UIKit.FitLine(verdict, 34f, 20f);

            float y = -64f;
            RectTransform starRow = null;
            if (chapter)
            {
                starRow = UIKit.Stars(Body, "Stars", stars, 92f, 3, 18f);
                UIKit.Pin(starRow, new Vector2(0.5f, 1f), new Vector2(0f, y), starRow.sizeDelta);
                y -= 108f;
            }

            Image plate = UIKit.Card(Body, "Record");
            var plateRect = UIKit.Pin((RectTransform)plate.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(560f, 150f));
            RectTransform lines = UIKit.Content(plate, 10f);
            UIKit.Layout<VerticalLayoutGroup>(lines, 4f).childForceExpandWidth = true;
            PlayerState me = Manager.ViewerState;
            Line(lines, "Days taken", days.ToString());
            if (me != null)
            {
                Line(lines, "Creatures slain", me.creaturesKilled.ToString("N0"));
                Line(lines, "Battles won", me.battlesWon.ToString());
                Line(lines, "Towns held", me.towns.Count.ToString());
            }
            plateRect.sizeDelta = new Vector2(560f, 24f + lines.childCount * 30f);
            y -= plateRect.sizeDelta.y + 14f;

            if (chapter)
            {
                TextMeshProUGUI record = UIKit.Label(Body, "Thresholds",
                    $"{UIKit.StarText(3)} in {scenario.ThreeStarDays} days   {UIKit.StarText(2)} in {scenario.TwoStarDays} days",
                    21f, UIKit.Dim, TextAlignmentOptions.Center);
                UIKit.Pin((RectTransform)record.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(760f, 30f));
                y -= 34f;
            }
            // The window is as tall as what it says, around the middle of the screen.
            Window.sizeDelta = new Vector2(Window.sizeDelta.x, Mathf.Max(400f, 70f - y + 24f + ButtonRow + 40f));

            Button leave = Answer(online ? "Back to the Room" : "Back to the Title", Leave);
            UIKit.Fit((RectTransform)leave.transform, 300f, ButtonRow - 4f);
            Open();
            if (starRow != null)
            {
                // Only once the window is open: a coroutine cannot start on an object that is off.
                StartCoroutine(PopStars(starRow));
            }
        }

        /// <summary>The way out of a finished game: to the room of an online game, else to the title screen.</summary>
        public void Leave()
        {
            Close();
            if (Manager.IsOnlineGame)
            {
                Manager.HeroesUI.LeaveToRoom();
            }
            else
            {
                Manager.ReturnToTitle();
            }
        }

        private static void Line(RectTransform parent, string what, string value)
        {
            RectTransform row = UIKit.Rect(parent, what);
            UIKit.Fit(row, 0f, 26f, true);
            TextMeshProUGUI name = UIKit.Label(row, "Name", what, 21f, UIKit.Dim, TextAlignmentOptions.MidlineLeft);
            UIKit.Stretch((RectTransform)name.transform, 8f, 0f, 0f, 0f);
            TextMeshProUGUI amount = UIKit.Label(row, "Value", value, 21f, UIKit.Ink, TextAlignmentOptions.MidlineRight);
            UIKit.Stretch((RectTransform)amount.transform, 0f, 0f, 8f, 0f);
        }

        /// <summary>The stars come in one after the other.</summary>
        private IEnumerator PopStars(RectTransform row)
        {
            foreach (Transform star in row)
            {
                star.localScale = Vector3.zero;
            }
            yield return new WaitForSecondsRealtime(0.25f);
            foreach (Transform star in row)
            {
                float t = 0f;
                while (t < 1f && star != null)
                {
                    t = Mathf.MoveTowards(t, 1f, Time.unscaledDeltaTime / 0.22f);
                    float overshoot = 1f + Mathf.Sin(t * Mathf.PI) * 0.25f;
                    star.localScale = Vector3.one * t * overshoot;
                    yield return null;
                }
                if (star != null)
                {
                    star.localScale = Vector3.one;
                }
            }
        }
    }

    /// <summary>
    /// A message with one way out, for anything that only has to be read, however long: its words scroll. The credits
    /// of the game are shown in it, their headings in gold.
    /// </summary>
    public sealed class MessageBox : Dialog
    {
        private RectTransform lines;
        private ScrollRect scroll;
        private System.Action closed;

        public static MessageBox Make(Transform parent, HeroesGameManager manager)
        {
            MessageBox box = Build<MessageBox>(parent, manager, "Message", new Vector2(980f, 800f), "");
            Image page = UIKit.Parchment(box.Body, "Page");
            UIKit.Stretch((RectTransform)page.transform);
            Vector4 inset = UIKit.Inset(page);
            box.lines = UIKit.Scroll(page.transform, "Lines", out box.scroll);
            UIKit.Stretch((RectTransform)box.scroll.transform, inset.x + 8f, inset.y + 4f, inset.z + 8f, inset.w + 4f);
            VerticalLayoutGroup layout = UIKit.Layout<VerticalLayoutGroup>(box.lines, 6f, new RectOffset(4, 12, 6, 12));
            layout.childForceExpandWidth = true;
            return box;
        }

        public void Show(string title, string text, string button = "Very Well", System.Action onClose = null)
        {
            UIKit.Clear(lines);
            ClearButtons();
            Heading.text = title;
            Paragraph(text);
            closed = onClose;
            Answer(button, Close);
            Open();
            scroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>The credits: a line in capitals is a heading, the lines after it until an empty one a paragraph.</summary>
        public void ShowCredits(string credits)
        {
            UIKit.Clear(lines);
            ClearButtons();
            Heading.text = "Credits";
            var paragraph = new StringBuilder();
            foreach (string raw in (credits ?? "").Replace("\r", "").Split('\n'))
            {
                string line = raw.Trim();
                bool heading = line.Length > 0 && line == line.ToUpperInvariant() && HasLetters(line);
                if (line.Length == 0 || heading)
                {
                    Flush(paragraph);
                }
                if (heading)
                {
                    TextMeshProUGUI title = UIKit.Title(lines, "Heading", line, 25f, new Color(0.45f, 0.1f, 0.06f), TextAlignmentOptions.Bottom);
                    UIKit.Fit((RectTransform)title.transform, 0f, lines.childCount > 0 ? 56f : 40f, true);
                    RectTransform rule = UIKit.Divider(lines, "Rule", 14f);
                    UIKit.Fit(rule, 0f, 14f, true);
                }
                else if (line.Length > 0)
                {
                    paragraph.Append(paragraph.Length > 0 ? " " : "").Append(line);
                }
            }
            Flush(paragraph);
            closed = null;
            Answer("Close", Close);
            Open();
            StartCoroutine(Top());
        }

        private static bool HasLetters(string line)
        {
            foreach (char c in line)
            {
                if (char.IsLetter(c))
                {
                    return true;
                }
            }
            return false;
        }

        private void Flush(StringBuilder paragraph)
        {
            if (paragraph.Length == 0)
            {
                return;
            }
            Paragraph(paragraph.ToString());
            paragraph.Length = 0;
        }

        private void Paragraph(string text)
        {
            TextMeshProUGUI words = UIKit.Label(lines, "Text", text, 22f, UIKit.InkOnParchment, TextAlignmentOptions.TopLeft);
            words.textWrappingMode = TextWrappingModes.Normal;
        }

        private IEnumerator Top()
        {
            yield return null;
            scroll.verticalNormalizedPosition = 1f;
        }

        public override void Close()
        {
            base.Close();
            System.Action then = closed;
            closed = null;
            then?.Invoke();
        }
    }

    /// <summary>
    /// A row of the seven places of an army, shared by the town and hero screens: each creature's portrait with how many
    /// on a small plate, a glow under the pointer and around the stack picked up. Clicking one stack and then another
    /// place moves, swaps or joins them.
    /// </summary>
    public sealed class ArmyRow : MonoBehaviour
    {
        private readonly List<Image> slots = new List<Image>();
        private readonly List<Image> pictures = new List<Image>();
        private readonly List<TextMeshProUGUI> counts = new List<TextMeshProUGUI>();
        private readonly List<Image> halos = new List<Image>();
        private HeroesGameManager manager;
        private int holder;

        /// <summary>The slot the player has picked up creatures from, shared by every row on screen.</summary>
        public static (ArmyRow row, int slot) Picked { get; private set; }

        public int Holder => holder;

        public static ArmyRow Make(Transform parent, HeroesGameManager manager, float size)
        {
            RectTransform rect = UIKit.Rect(parent, "Army");
            var row = rect.gameObject.AddComponent<ArmyRow>();
            row.manager = manager;
            HorizontalLayoutGroup layout = UIKit.Layout<HorizontalLayoutGroup>(rect, Mathf.Round(size * 0.1f));
            layout.childAlignment = TextAnchor.MiddleLeft;
            for (int i = 0; i < HeroData.ArmySlots; i++)
            {
                int index = i;
                RectTransform cell = UIKit.Rect(rect, $"Place{i}");
                UIKit.Fit(cell, size, size);
                Image halo = UIKit.Halo(cell, "Halo", new Color(1f, 0.85f, 0.45f, 0f));
                UIKit.Stretch((RectTransform)halo.transform, -size * 0.22f, -size * 0.22f, -size * 0.22f, -size * 0.22f);
                Image slot = UIKit.Slot(cell, "Slot");
                UIKit.Stretch((RectTransform)slot.transform);
                var button = slot.gameObject.AddComponent<Button>();
                button.targetGraphic = slot;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                if (UIKit.Art != null && UIKit.Art.slotHover != null)
                {
                    button.transition = Selectable.Transition.SpriteSwap;
                    button.spriteState = new SpriteState { highlightedSprite = UIKit.Art.slotHover, pressedSprite = UIKit.Art.slotSelected };
                }
                else
                {
                    button.colors = UIKit.Tint();
                }
                button.onClick.AddListener(() => row.Clicked(index));
                slot.gameObject.AddComponent<Clicker>();
                Gamebox.UI.PressFeedback.Attach(slot.gameObject, 1.05f, 0.95f);

                Image picture = UIKit.Sprite(slot.transform, "Portrait", null, Color.white);
                picture.preserveAspect = true;
                UIKit.Stretch((RectTransform)picture.transform, 4f, 4f, 4f, 4f);
                picture.enabled = false;

                Image plate = UIKit.Recess(slot.transform, "Plate");
                float plateHeight = Mathf.Max(22f, size * 0.3f);
                UIKit.Pin((RectTransform)plate.transform, new Vector2(1f, 0f), new Vector2(-2f, 2f), new Vector2(size * 0.62f, plateHeight));
                TextMeshProUGUI count = UIKit.Label(plate.transform, "Count", "", plateHeight * 0.72f, UIKit.Ink, TextAlignmentOptions.Center);
                UIKit.Stretch((RectTransform)count.transform, 3f, 0f, 3f, 0f);
                UIKit.FitLine(count, plateHeight * 0.72f, 11f);

                row.slots.Add(slot);
                row.pictures.Add(picture);
                row.counts.Add(count);
                row.halos.Add(halo);
            }
            return row;
        }

        public void Bind(int which)
        {
            holder = which;
            Refresh();
        }

        public void Refresh()
        {
            Army army = ArmyOf(holder);
            for (int i = 0; i < slots.Count; i++)
            {
                ArmySlot slot = army != null ? army.slots[i] : null;
                bool full = slot != null && !slot.IsEmpty;
                pictures[i].enabled = full;
                counts[i].transform.parent.gameObject.SetActive(full);
                counts[i].text = full ? slot.count.ToString("N0") : "";
                if (full)
                {
                    CreatureDef def = slot.Def;
                    Sprite face = manager.Art.Portrait(def.Id);
                    pictures[i].sprite = face != null ? face : manager.Art.Icon("monster");
                    Tooltip.Attach(slots[i].gameObject, $"{slot.count} {(slot.count == 1 ? def.Name : def.Plural)}", Describe(def), face);
                }
                else
                {
                    Tooltip.Attach(slots[i].gameObject, "An empty place in the ranks.");
                }
                bool picked = Picked.row == this && Picked.slot == i;
                if (UIKit.Art != null && UIKit.Art.slotSelected != null)
                {
                    slots[i].sprite = picked ? UIKit.Art.slotSelected : UIKit.Art.slot;
                }
                Color glow = halos[i].color;
                glow.a = picked ? 0.85f : 0f;
                halos[i].color = glow;
            }
        }

        public static string Describe(CreatureDef def)
        {
            return $"{UIKit.Glyph("attack")} {def.Attack}   {UIKit.Glyph("defense")} {def.Defense}   " +
                   $"{UIKit.Glyph("damage")} {def.MinDamage}-{def.MaxDamage}\n" +
                   $"{UIKit.Glyph("health")} {def.Health}   {UIKit.Glyph("speed")} {def.Speed}{(def.Shots > 0 ? $"   {def.Shots} shots" : "")}" +
                   (string.IsNullOrEmpty(def.Description) ? "" : $"\n<i>{def.Description}</i>");
        }

        private Army ArmyOf(int which)
        {
            GameState state = manager.Game.State;
            if (Portfolio.Heroes.Holder.IsGarrison(which))
            {
                TownState town = state.Town(Portfolio.Heroes.Holder.TownOf(which));
                return town != null ? town.garrison : null;
            }
            HeroState hero = state.Hero(which);
            return hero != null ? hero.army : null;
        }

        /// <summary>First click picks a stack up, second puts it down (or swaps it, or joins it).</summary>
        private void Clicked(int slot)
        {
            Army army = ArmyOf(holder);
            if (army == null)
            {
                return;
            }
            if (Picked.row == null)
            {
                if (!army.slots[slot].IsEmpty)
                {
                    Picked = (this, slot);
                    Refresh();
                }
                return;
            }
            if (Picked.row == this && Picked.slot == slot)
            {
                Picked = (null, -1);
                Refresh();
                return;
            }
            (ArmyRow from, int fromSlot) = Picked;
            Picked = (null, -1);
            if (manager.HeroesUI == null || manager.HeroesUI.CanCommand)
            {
                manager.Commands.MoveArmy(from.holder, fromSlot, holder, slot, 0);
            }
            from.Refresh();
            Refresh();
        }

        public static void Drop()
        {
            Picked = (null, -1);
        }
    }
}
