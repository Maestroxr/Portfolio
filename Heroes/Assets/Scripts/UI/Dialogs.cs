using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>A window that darkens the map behind it and holds a title, a body and a row of answers.</summary>
    public class Dialog : MonoBehaviour
    {
        protected HeroesGameManager Manager;
        protected RectTransform Body;
        protected RectTransform Buttons;
        protected TextMeshProUGUI Heading;
        private Image shade;

        /// <summary>Builds the empty window, the size asked for, in the middle of the screen.</summary>
        protected static T Build<T>(Transform parent, HeroesGameManager manager, string name, Vector2 size, string title)
            where T : Dialog
        {
            RectTransform holder = UIKit.Rect(parent, name);
            UIKit.Stretch(holder);
            var dialog = holder.gameObject.AddComponent<T>();
            dialog.Manager = manager;

            dialog.shade = UIKit.Sprite(holder, "Shade", null, new Color(0f, 0f, 0f, 0.55f));
            UIKit.Stretch((RectTransform)dialog.shade.transform);
            dialog.shade.raycastTarget = true;

            Image frame = UIKit.Frame(holder, "Window");
            var window = (RectTransform)frame.transform;
            UIKit.Pin(window, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            window.pivot = new Vector2(0.5f, 0.5f);

            dialog.Heading = UIKit.Title(window, "Heading", title, 34f, UIKit.Gold);
            UIKit.Pin((RectTransform)dialog.Heading.transform, new Vector2(0.5f, 1f), new Vector2(0f, -28f),
                new Vector2(size.x - 90f, 42f));

            dialog.Body = UIKit.Rect(window, "Body");
            UIKit.Stretch(dialog.Body, 42f, 96f, 42f, 82f);

            dialog.Buttons = UIKit.Rect(window, "Buttons");
            UIKit.Pin(dialog.Buttons, new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(size.x - 84f, 60f));
            HorizontalLayoutGroup layout = UIKit.Layout<HorizontalLayoutGroup>(dialog.Buttons, 14f);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;

            holder.gameObject.SetActive(false);
            return dialog;
        }

        public bool IsOpen => gameObject.activeSelf;

        public virtual void Close()
        {
            gameObject.SetActive(false);
        }

        protected void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        protected void ClearBody()
        {
            for (int i = Body.childCount - 1; i >= 0; i--)
            {
                Destroy(Body.GetChild(i).gameObject);
            }
        }

        protected void ClearButtons()
        {
            for (int i = Buttons.childCount - 1; i >= 0; i--)
            {
                Destroy(Buttons.GetChild(i).gameObject);
            }
        }

        protected Button Answer(string text, System.Action onClick)
        {
            Button button = UIKit.Push(Buttons, text, text, onClick);
            UIKit.Fit((RectTransform)button.transform, 0f, 56f, true);
            return button;
        }
    }

    /// <summary>
    /// The question the rules are waiting on: which skill a hero takes on a level up, or whether to take the gold or
    /// the wisdom of a find. Nothing else in the game moves until it is answered.
    /// </summary>
    public sealed class ChoiceBox : Dialog
    {
        public static ChoiceBox Make(Transform parent, HeroesGameManager manager)
        {
            return Build<ChoiceBox>(parent, manager, "Choice", new Vector2(760f, 420f), "A Choice");
        }

        public void Show(PendingChoice pending)
        {
            if (pending == null)
            {
                Close();
                return;
            }
            ClearBody();
            ClearButtons();
            HeroState hero = Manager.Game.State.Hero(pending.hero);
            switch (pending.kind)
            {
                case ChoiceKind.LevelUp:
                    LevelUp(pending, hero);
                    break;
                case ChoiceKind.Treasure:
                    Treasure(pending, hero);
                    break;
                default:
                    Arena(pending, hero);
                    break;
            }
            Open();
        }

        private void LevelUp(PendingChoice pending, HeroState hero)
        {
            Heading.text = hero != null ? $"{hero.Name} reaches level {hero.level}" : "A new level";
            string raised = pending.stat >= 0 ? $"+1 {HeroData.StatName((PrimaryStat)pending.stat)}. " : "";
            Note($"{raised}Choose what else the hero has learned.");
            for (int i = 0; i < pending.options.Length; i++)
            {
                int option = i;
                int skill = pending.options[i];
                if (skill < 0)
                {
                    continue;
                }
                SkillDef def = HeroData.Skill((SkillId)skill);
                int level = hero != null ? hero.SkillLevel((SkillId)skill) : 0;
                string title = $"{HeroData.SkillLevelName(level + 1)} {def.Name}";
                Button button = Answer(title, () =>
                {
                    Manager.Commands.Choose(option);
                    Close();
                });
                Tooltip.Attach(button.gameObject, def.Description);
            }
        }

        private void Treasure(PendingChoice pending, HeroState hero)
        {
            Heading.text = "A Chest of Treasure";
            Note("Gold for the coffers, or the lessons of the road?");
            Answer($"{pending.value} Gold", () =>
            {
                Manager.Commands.Choose(0);
                Close();
            });
            Answer($"{pending.value / 2} Experience", () =>
            {
                Manager.Commands.Choose(1);
                Close();
            });
        }

        private void Arena(PendingChoice pending, HeroState hero)
        {
            Heading.text = "The Arena";
            Note("The masters of the arena offer their training.");
            Answer("+2 Attack", () =>
            {
                Manager.Commands.Choose(0);
                Close();
            });
            Answer("+2 Defense", () =>
            {
                Manager.Commands.Choose(1);
                Close();
            });
        }

        private void Note(string text)
        {
            TextMeshProUGUI label = UIKit.Label(Body, "Note", text, 26f, UIKit.Ink, TextAlignmentOptions.Top);
            UIKit.Stretch((RectTransform)label.transform);
        }
    }

    /// <summary>What the scenario came to: won or lost, in how many days, and how many stars that is worth.</summary>
    public sealed class ResultsBox : Dialog
    {
        public static ResultsBox Make(Transform parent, HeroesGameManager manager)
        {
            return Build<ResultsBox>(parent, manager, "Results", new Vector2(820f, 520f), "");
        }

        public void Show(bool won, int stars, int days)
        {
            ClearBody();
            ClearButtons();
            Heading.text = won ? "Victory" : "Defeat";
            Heading.color = won ? UIKit.Gold : UIKit.Bad;

            RectTransform row = UIKit.Rect(Body, "Stars");
            UIKit.Pin(row, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(360f, 90f));
            HorizontalLayoutGroup layout = UIKit.Layout<HorizontalLayoutGroup>(row, 16f);
            layout.childAlignment = TextAnchor.MiddleCenter;
            for (int i = 0; i < 3; i++)
            {
                Image star = UIKit.Sprite(row, $"Star{i}", i < stars ? Manager.Art.star : Manager.Art.starEmpty, Color.white);
                UIKit.Fit((RectTransform)star.transform, 84f, 84f);
            }

            HeroesLevel scenario = Manager.Scenario;
            var text = new System.Text.StringBuilder();
            text.Append(won ? "The land is yours." : "Your banners have fallen.").Append('\n');
            text.Append($"Days taken: {days}").Append('\n');
            if (scenario != null && won)
            {
                text.Append($"Three stars in {scenario.ThreeStarDays} days, two in {scenario.TwoStarDays}.");
            }
            TextMeshProUGUI note = UIKit.Label(Body, "Note", text.ToString(), 26f, UIKit.Ink, TextAlignmentOptions.Top);
            UIKit.Stretch((RectTransform)note.transform, 0f, 0f, 0f, 110f);

            Answer("Back to the Map", () =>
            {
                Close();
                Manager.ReturnToTitle();
            });
        }
    }

    /// <summary>A plain message with one way out, for anything that only has to be read.</summary>
    public sealed class MessageBox : Dialog
    {
        private TextMeshProUGUI note;

        public static MessageBox Make(Transform parent, HeroesGameManager manager)
        {
            MessageBox box = Build<MessageBox>(parent, manager, "Message", new Vector2(720f, 380f), "");
            box.note = UIKit.Label(box.Body, "Note", "", 26f, UIKit.Ink, TextAlignmentOptions.Top);
            UIKit.Stretch((RectTransform)box.note.transform);
            return box;
        }

        public void Show(string title, string text, string button = "Very Well", System.Action onClose = null)
        {
            ClearButtons();
            Heading.text = title;
            note.text = text;
            Answer(button, () =>
            {
                Close();
                onClose?.Invoke();
            });
            Open();
        }
    }

    /// <summary>A row of army slots that can be picked from and moved between, shared by the town and hero screens.</summary>
    public sealed class ArmyRow : MonoBehaviour
    {
        private readonly List<Image> slots = new List<Image>();
        private readonly List<TextMeshProUGUI> counts = new List<TextMeshProUGUI>();
        private readonly List<Image> icons = new List<Image>();
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
            HorizontalLayoutGroup layout = UIKit.Layout<HorizontalLayoutGroup>(rect, 8f);
            layout.childAlignment = TextAnchor.MiddleCenter;
            for (int i = 0; i < HeroData.ArmySlots; i++)
            {
                int index = i;
                Image slot = UIKit.Slot(rect, $"Slot{i}");
                UIKit.Fit((RectTransform)slot.transform, size, size);
                var button = slot.gameObject.AddComponent<Button>();
                button.targetGraphic = slot;
                button.colors = UIKit.Tint();
                button.onClick.AddListener(() => row.Clicked(index));
                slot.gameObject.AddComponent<Clicker>();

                Image icon = UIKit.Sprite(slot.transform, "Icon", null, Color.white);
                UIKit.Stretch((RectTransform)icon.transform, 8f, 14f, 8f, 8f);
                icon.preserveAspect = true;
                icon.enabled = false;

                TextMeshProUGUI count = UIKit.Label(slot.transform, "Count", "", size * 0.24f, UIKit.Ink,
                    TextAlignmentOptions.BottomRight);
                UIKit.Stretch((RectTransform)count.transform, 4f, 4f, 6f, 4f);
                count.textWrappingMode = TextWrappingModes.NoWrap;

                row.slots.Add(slot);
                row.icons.Add(icon);
                row.counts.Add(count);
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
                icons[i].enabled = full;
                counts[i].text = full ? slot.count.ToString() : "";
                if (full)
                {
                    CreatureDef def = slot.Def;
                    icons[i].sprite = manager.Art.Icon("monster");
                    icons[i].color = Faded(def);
                    Tooltip.Attach(slots[i].gameObject, Describe(def, slot.count));
                }
                else
                {
                    Tooltip.Attach(slots[i].gameObject, "An empty place in the ranks.");
                }
                bool picked = Picked.row == this && Picked.slot == i;
                slots[i].color = picked ? new Color(1f, 0.92f, 0.6f) : Color.white;
            }
        }

        private static Color Faded(CreatureDef def)
        {
            switch (def.Faction)
            {
                case Faction.Necropolis: return new Color(0.72f, 0.75f, 0.85f);
                case Faction.Stronghold: return new Color(0.9f, 0.6f, 0.45f);
                case Faction.Castle: return new Color(0.95f, 0.88f, 0.7f);
                default: return new Color(0.7f, 0.85f, 0.65f);
            }
        }

        private static string Describe(CreatureDef def, int count)
        {
            return $"{count} {(count == 1 ? def.Name : def.Plural)}\n" +
                   $"Attack {def.Attack}, Defense {def.Defense}\n" +
                   $"Damage {def.MinDamage}-{def.MaxDamage}, Health {def.Health}\n" +
                   $"Speed {def.Speed}{(def.Shots > 0 ? $", {def.Shots} shots" : "")}";
        }

        private Army ArmyOf(int which)
        {
            GameState state = manager.Game.State;
            if (Holder_IsGarrison(which))
            {
                TownState town = state.Town(Portfolio.Heroes.Holder.TownOf(which));
                return town != null ? town.garrison : null;
            }
            HeroState hero = state.Hero(which);
            return hero != null ? hero.army : null;
        }

        private static bool Holder_IsGarrison(int which)
        {
            return Portfolio.Heroes.Holder.IsGarrison(which);
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
            manager.Commands.MoveArmy(from.holder, fromSlot, holder, slot, 0);
            from.Refresh();
            Refresh();
        }

        public static void Drop()
        {
            Picked = (null, -1);
        }
    }
}
