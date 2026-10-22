using System.Collections.Generic;
using Gamebox.Online;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// What a phone shows while it plays as a remote control of a room's shared screen
    /// (<see cref="OnlineGameController.BuildRemotePanel"/>): the map stays the control under the panel's frame (a hero
    /// is sent where the player taps, a town opens as on any device), and the panel adds, in a few thin lines under the
    /// frame's header, what the frame hides of the adventure map: the seat's treasury and the date, its hero in hand
    /// with his movement, mana and army, a line on what the rules wait for, and the commands of the moment: Next Hero
    /// and End Turn on the map, the options of a question (a level up, a chest, the arena), Auto combat, Wait, Defend
    /// and Retreat in a battle. Everything goes through the same commands the interface sends
    /// (<see cref="HeroesGameManager.Commands"/>), so the device plays its seat as it always did; what to show comes
    /// from <see cref="RemoteView"/>, worked out from the game every client holds.
    /// </summary>
    public sealed class RemotePanel : MonoBehaviour
    {
        private const float LineHeight = 40f;
        private const float ButtonHeight = 76f;
        private const float Gap = 10f;
        private const float RefreshEvery = 0.2f;
        /// <summary>The height of the three lines over the commands, with the kit's spacing.</summary>
        private const float LinesHeight = 3f * (LineHeight + 12f);

        /// <summary>The height the panel takes under the frame's header during a battle: its row of battle commands alone, with the kit's spacing.</summary>
        public const float BattleRowHeight = ButtonHeight + 12f + 10f;

        private HeroesOnlineController online;
        private OverlayKit kit;
        private TMP_Text realm, date, hero, note;
        private RectTransform realmLine, heroLine, noteLine;
        private RectTransform row;
        private bool linesShown = true;
        private Button nextHero, endTurn, auto, wait, defend, retreat, onward;
        private readonly Button[] options = new Button[2];
        private readonly List<Button> showing = new List<Button>();
        private float nextRefresh;
        /// <summary>The stack a first press of Retreat was for: a second press on the same stack retreats.</summary>
        private int retreatArmedFor = -1;

        /// <summary>The panel on this device, while the remote frame shows one; null otherwise.</summary>
        public static RemotePanel Current { get; private set; }

        /// <summary>What the panel last offered (<see cref="RemoteView.ModeOf"/>).</summary>
        public RemoteMode Mode { get; private set; }

        private HeroesGameManager Manager => online != null ? online.Heroes : null;

        private HeroesGame Game => Manager != null ? Manager.Game : null;

        /// <summary>The seat this device plays.</summary>
        private int Seat => Manager != null ? Manager.LocalSeat : -1;

        /// <summary>Builds the panel into the content of the remote frame, under its header.</summary>
        public static RemotePanel Build(HeroesOnlineController online, RectTransform content, RemotePanelKit kit)
        {
            var panel = content.gameObject.AddComponent<RemotePanel>();
            panel.online = online;
            panel.kit = kit.Kit;
            OverlayLook look = kit.Kit.Look;
            panel.realmLine = kit.Section("Realm", LineHeight);
            panel.realm = panel.Line(panel.realmLine, "Resources", 26f, look.text, TextAlignmentOptions.MidlineLeft);
            panel.date = panel.Line(panel.realmLine, "Date", 26f, look.muted, TextAlignmentOptions.MidlineRight);
            panel.heroLine = kit.Section("Hero", LineHeight);
            panel.hero = panel.Line(panel.heroLine, "Hero", 26f, look.text, TextAlignmentOptions.MidlineLeft);
            panel.noteLine = kit.Section("Note", LineHeight);
            panel.note = panel.Line(panel.noteLine, "Note", 28f, look.accent, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            panel.row = kit.Section("Commands", ButtonHeight);
            panel.Buttons(look);
            Current = panel;
            panel.Refresh();
            return panel;
        }

        private TMP_Text Line(RectTransform section, string lineName, float size, Color color, TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal)
        {
            TMP_Text text = kit.Text(section, lineName, "", size, color, alignment, style);
            OverlayKit.Stretch(text.rectTransform);
            OverlayKit.Fit(text, size);
            text.raycastTarget = false;
            return text;
        }

        /// <summary>Every button of the panel, made once; which of them show follows the game.</summary>
        private void Buttons(OverlayLook look)
        {
            nextHero = Make("Next Hero", look.row, PressNextHero);
            endTurn = Make("End Turn", look.good, PressEndTurn);
            auto = Make("Auto combat", look.accent, PressAuto);
            wait = Make("Wait", look.row, PressWait);
            defend = Make("Defend", look.row, PressDefend);
            retreat = Make("Retreat", look.warn, PressRetreat);
            onward = Make("Onward", look.accent, () => PressChoice(0));
            for (int i = 0; i < options.Length; i++)
            {
                int option = i;
                options[i] = Make("", look.accent, () => PressChoice(option));
            }
        }

        private Button Make(string label, Color color, System.Action pressed)
        {
            Button button = kit.MakeButton(row, label.Length > 0 ? label : "Option", color, new Vector2(0f, ButtonHeight), pressed);
            button.gameObject.SetActive(false);
            return button;
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + RefreshEvery;
                Refresh();
            }
        }

        // ------------------------------------------------------------------ what the panel shows

        /// <summary>Writes the lines and lays out the commands of the moment from the game as it stands.</summary>
        public void Refresh()
        {
            HeroesGame game = Game;
            GameState state = game?.State;
            int seat = Seat;
            PlayerState player = state?.Player(seat);
            if (game == null || player == null)
            {
                realm.text = "";
                date.text = "";
                hero.text = "";
                note.text = "";
                Mode = RemoteMode.Watching;
                Lines(true);
                Lay();
                return;
            }
            realm.text = RemoteView.ResourcesLine(player, Words.T);
            date.text = Words.F("Month {0}, Week {1}, Day {2}", state.Month, state.WeekOfMonth, state.DayOfWeek);
            HeroState inHand = RemoteView.HeroInHand(state, seat, Manager.Selected != null ? Manager.Selected.id : -1);
            hero.text = inHand != null ? HeroLine(game, inHand) : Words.T("No heroes. Hire one in a tavern.");

            RemoteMode mode = RemoteView.ModeOf(game, seat);
            if (mode != Mode)
            {
                retreatArmedFor = -1;
            }
            Mode = mode;
            HeroesUI ui = Manager.HeroesUI;
            if (ui != null && ui.WindowOpen)
            {
                // A window of the game lies over the map (a town, a hero, a dwelling, the results of a battle), pulled in
                // between the frame's header and footer: the panel keeps off it, and nothing of the panel's is to be pressed.
                Lines(false);
                Lay();
                return;
            }
            // In a battle the bar has the stack in hand and the heroes: only the commands stay, close under the header,
            // and the field is framed under them.
            Lines(mode != RemoteMode.Fighting);
            bool may = Manager.OnlineDeciding();
            switch (mode)
            {
                case RemoteMode.Choosing:
                    Choice(state, state.pending[0], may);
                    break;
                case RemoteMode.Fighting:
                    Fight(game, may);
                    break;
                case RemoteMode.Moving:
                    note.text = Words.T("Your turn");
                    UIKit.Enable(nextHero, may && Manager.NextHero() != null);
                    UIKit.Enable(endTurn, may);
                    Lay(nextHero, endTurn);
                    break;
                default:
                    note.text = game.IsOver ? Words.T("Game over") : "";
                    Lay();
                    break;
            }
        }

        /// <summary>Shows or hides the three lines over the commands; without them the commands move up under the header.</summary>
        private void Lines(bool on)
        {
            if (on == linesShown && realmLine.gameObject.activeSelf == on)
            {
                return;
            }
            linesShown = on;
            realmLine.gameObject.SetActive(on);
            heroLine.gameObject.SetActive(on);
            noteLine.gameObject.SetActive(on);
            row.anchoredPosition = new Vector2(0f, on ? -LinesHeight : 0f);
        }

        /// <summary>The hero in hand: his name and level, movement and mana, and his army.</summary>
        private string HeroLine(HeroesGame game, HeroState inHand)
        {
            string army = RemoteView.ArmyLine(inHand.army, Words.T);
            return $"{Words.Name(inHand.Name)}, {Words.F("Level {0}", inHand.level)}   " +
                   $"{Words.T("Movement")} {inHand.movement}/{inHand.maxMovement}   {Words.T("Mana")} {inHand.mana}/{game.MaxMana(inHand)}" +
                   (army.Length > 0 ? "   " + army : "");
        }

        /// <summary>The question the rules put to the seat, with its options as buttons (as the choice dialog words them).</summary>
        private void Choice(GameState state, PendingChoice pending, bool may)
        {
            HeroState chosen = state.Hero(pending.hero);
            int shown = 0;
            switch (pending.kind)
            {
                case ChoiceKind.LevelUp:
                    note.text = chosen != null ? Words.F("{0} reaches level {1}", Words.Name(chosen.Name), chosen.level) : Words.T("A New Level");
                    for (int i = 0; i < pending.options.Length && shown < options.Length; i++)
                    {
                        int skill = pending.options[i];
                        if (skill < 0)
                        {
                            continue;
                        }
                        int level = chosen != null ? chosen.SkillLevel((SkillId)skill) : 0;
                        Label(options[shown], i, Words.SkillName(level + 1, HeroData.Skill((SkillId)skill).Name));
                        shown++;
                    }
                    break;
                case ChoiceKind.Treasure:
                    note.text = Words.T("A Chest of Treasure");
                    Label(options[0], 0, Words.F("{0} Gold", pending.options[0].ToString("N0")));
                    Label(options[1], 1, Words.F("{0} Experience", pending.options[1].ToString("N0")));
                    shown = 2;
                    break;
                default:
                    note.text = Words.T("The Arena");
                    Label(options[0], 0, Words.T("+2 Attack"));
                    Label(options[1], 1, Words.T("+2 Defense"));
                    shown = 2;
                    break;
            }
            if (shown == 0)
            {
                UIKit.Enable(onward, may);
                Lay(onward);
                return;
            }
            for (int i = 0; i < shown; i++)
            {
                UIKit.Enable(options[i], may);
            }
            Lay(shown == 1 ? new[] { options[0] } : options);
        }

        /// <summary>The option a button answers with, kept on its name, and its words.</summary>
        private static void Label(Button button, int option, string words)
        {
            button.name = $"Option {option}";
            TMP_Text label = OverlayKit.LabelOf(button);
            if (label != null)
            {
                label.text = words;
            }
        }

        /// <summary>The stack of the seat's that acts, and the commands of a battle.</summary>
        private void Fight(HeroesGame game, bool may)
        {
            BattleStack current = game.Battle.Current;
            string stack = current != null && current.Def != null ? $"{current.count} {Words.T(current.Def.NameFor(current.count))}" : "";
            note.text = stack.Length > 0 ? $"{Words.T("Your move")}  -  {stack}" : Words.T("Your move");
            if (current == null || current.id != retreatArmedFor)
            {
                retreatArmedFor = -1;
            }
            UIKit.Enable(auto, may);
            UIKit.Enable(wait, may && RemoteView.MayWait(game, Seat));
            UIKit.Enable(defend, may);
            UIKit.Enable(retreat, may && RemoteView.MayRetreat(game, Seat));
            TMP_Text label = OverlayKit.LabelOf(retreat);
            if (label != null)
            {
                label.text = Words.T(retreatArmedFor >= 0 ? "Retreat?" : "Retreat");
            }
            Lay(auto, wait, defend, retreat);
        }

        /// <summary>Shows <paramref name="buttons"/> side by side across the row, equally wide, and hides every other.</summary>
        private void Lay(params Button[] buttons)
        {
            showing.Clear();
            showing.AddRange(buttons);
            foreach (Button button in new[] { nextHero, endTurn, auto, wait, defend, retreat, onward, options[0], options[1] })
            {
                int index = showing.IndexOf(button);
                button.gameObject.SetActive(index >= 0);
                if (index < 0)
                {
                    continue;
                }
                var rect = (RectTransform)button.transform;
                float from = (float)index / showing.Count;
                float to = (float)(index + 1) / showing.Count;
                rect.anchorMin = new Vector2(from, 0f);
                rect.anchorMax = new Vector2(to, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = new Vector2(index == 0 ? 0f : Gap / 2f, 0f);
                rect.offsetMax = new Vector2(index == showing.Count - 1 ? 0f : -Gap / 2f, 0f);
            }
        }

        // ------------------------------------------------------------------ the commands (the tours press them too)

        /// <summary>Puts the next hero with movement left in hand and brings the camera to him.</summary>
        public void PressNextHero()
        {
            HeroesGameManager manager = Manager;
            if (manager != null && Mode == RemoteMode.Moving)
            {
                manager.Select(manager.NextHero());
            }
        }

        /// <summary>Ends the seat's turn on the map.</summary>
        public void PressEndTurn()
        {
            HeroesGameManager manager = Manager;
            if (manager != null && Mode == RemoteMode.Moving && manager.OnlineDeciding())
            {
                manager.Commands.EndTurn();
            }
        }

        /// <summary>Answers the question the rules put to the seat with <paramref name="option"/>.</summary>
        public void PressChoice(int option)
        {
            HeroesGameManager manager = Manager;
            if (manager != null && Mode == RemoteMode.Choosing && manager.OnlineDeciding())
            {
                manager.Commands.Choose(option);
            }
        }

        /// <summary>One move of auto combat for the stack in hand: the computer's battle sense, never a retreat (<see cref="RemoteView.AutoMove"/>).</summary>
        public void PressAuto()
        {
            HeroesGameManager manager = Manager;
            GameCommand move = manager != null && Mode == RemoteMode.Fighting && manager.OnlineDeciding() ? RemoteView.AutoMove(Game, Seat) : null;
            if (move != null)
            {
                manager.Submit(move);
            }
        }

        public void PressWait()
        {
            HeroesGameManager manager = Manager;
            if (manager != null && Mode == RemoteMode.Fighting && manager.OnlineDeciding() && RemoteView.MayWait(Game, Seat))
            {
                manager.Commands.BattleWait(Game.Battle.current);
            }
        }

        public void PressDefend()
        {
            HeroesGameManager manager = Manager;
            if (manager != null && Mode == RemoteMode.Fighting && manager.OnlineDeciding())
            {
                manager.Commands.BattleDefend(Game.Battle.current);
            }
        }

        /// <summary>A retreat asks first: the first press arms the button ("Retreat?"), a second press for the same stack retreats.</summary>
        public void PressRetreat()
        {
            HeroesGameManager manager = Manager;
            if (manager == null || Mode != RemoteMode.Fighting || !manager.OnlineDeciding() || !RemoteView.MayRetreat(Game, Seat))
            {
                return;
            }
            int stack = Game.Battle.current;
            if (retreatArmedFor == stack)
            {
                retreatArmedFor = -1;
                manager.Commands.BattleRetreat();
            }
            else
            {
                retreatArmedFor = stack;
            }
            Refresh();
        }
    }
}
