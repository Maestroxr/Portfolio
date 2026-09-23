using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// The bar that appears along the bottom while a battle is fought on the map: whose turn it is, the order the
    /// stacks will act in, and what the stack in hand may do. The cells of the battlefield are painted for it, and the
    /// clicks on them become moves, blows and shots.
    /// </summary>
    public sealed class BattleBar : MonoBehaviour
    {
        private HeroesGameManager manager;
        private RectTransform root;
        private RectTransform order;
        private TextMeshProUGUI title;
        private TextMeshProUGUI detail;
        private TextMeshProUGUI round;
        private Button wait;
        private Button defend;
        private Button cast;
        private Button retreat;
        private SpellBox spellBox;

        private Dictionary<int, int> reach = new Dictionary<int, int>();
        private readonly List<int> targets = new List<int>();
        private readonly List<int> shots = new List<int>();
        private int shown = -1;
        private int hovered = -1;

        public static BattleBar Make(Transform parent, HeroesGameManager manager)
        {
            Image frame = UIKit.Frame(parent, "BattleBar");
            var bar = frame.gameObject.AddComponent<BattleBar>();
            bar.manager = manager;
            bar.root = UIKit.Pin((RectTransform)frame.transform, new Vector2(0.5f, 0f), new Vector2(0f, 252f),
                new Vector2(1120f, 168f));
            bar.root.pivot = new Vector2(0.5f, 0f);

            bar.title = UIKit.Title(bar.root, "Title", "", 28f, UIKit.Gold, TextAlignmentOptions.Left);
            UIKit.Pin((RectTransform)bar.title.transform, new Vector2(0f, 1f), new Vector2(28f, -22f), new Vector2(460f, 34f));
            bar.detail = UIKit.Label(bar.root, "Detail", "", 20f, UIKit.Ink, TextAlignmentOptions.TopLeft);
            UIKit.Pin((RectTransform)bar.detail.transform, new Vector2(0f, 1f), new Vector2(28f, -58f), new Vector2(460f, 60f));
            bar.round = UIKit.Label(bar.root, "Round", "", 22f, UIKit.Dim, TextAlignmentOptions.TopRight);
            UIKit.Pin((RectTransform)bar.round.transform, new Vector2(1f, 1f), new Vector2(-28f, -22f), new Vector2(240f, 28f));

            bar.order = UIKit.Rect(bar.root, "Order");
            UIKit.Pin(bar.order, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1120f, 50f));
            HorizontalLayoutGroup layout = UIKit.Layout<HorizontalLayoutGroup>(bar.order, 5f);
            layout.childAlignment = TextAnchor.MiddleCenter;

            RectTransform buttons = UIKit.Rect(bar.root, "Buttons");
            UIKit.Pin(buttons, new Vector2(1f, 1f), new Vector2(-28f, -48f), new Vector2(520f, 56f));
            HorizontalLayoutGroup row = UIKit.Layout<HorizontalLayoutGroup>(buttons, 8f);
            row.childAlignment = TextAnchor.MiddleRight;
            row.childForceExpandWidth = true;

            bar.wait = UIKit.Push(buttons, "Wait", "Wait", bar.Wait, 22f);
            UIKit.Fit((RectTransform)bar.wait.transform, 0f, 52f, true);
            bar.defend = UIKit.Push(buttons, "Defend", "Defend", bar.Defend, 22f);
            UIKit.Fit((RectTransform)bar.defend.transform, 0f, 52f, true);
            bar.cast = UIKit.Push(buttons, "Cast", "Spell", bar.OpenSpells, 22f);
            UIKit.Fit((RectTransform)bar.cast.transform, 0f, 52f, true);
            bar.retreat = UIKit.Push(buttons, "Retreat", "Retreat", bar.Retreat, 22f);
            UIKit.Fit((RectTransform)bar.retreat.transform, 0f, 52f, true);

            bar.spellBox = SpellBox.Make(parent, manager, bar);
            bar.root.gameObject.SetActive(false);
            return bar;
        }

        private HeroesGame Game => manager.Game;

        private BattleState State => Game != null ? Game.Battle : null;

        /// <summary>The spell the player picked and is now choosing a cell for, or None.</summary>
        public SpellId Casting { get; private set; } = SpellId.None;

        public void Begin(BattleState battle)
        {
            root.gameObject.SetActive(true);
            shown = -1;
            Casting = SpellId.None;
            SetRound(battle.round);
        }

        public void Hide()
        {
            root.gameObject.SetActive(false);
            spellBox?.Close();
            Casting = SpellId.None;
        }

        public void SetRound(int number)
        {
            round.text = $"Round {number}";
        }

        /// <summary>Keeps the bar telling the truth while the other side moves, without offering its buttons.</summary>
        private void Update()
        {
            BattleState battle = State;
            if (battle == null || !battle.active || !root.gameObject.activeSelf)
            {
                return;
            }
            BattleStack stack = battle.Current;
            if (stack == null)
            {
                return;
            }
            SetRound(battle.round);
            bool ours = battle.PlayerOf(stack.side) == manager.Viewer && !manager.HeroesUI.Busy;
            if (!ours)
            {
                CreatureDef def = Creatures.Get(stack.creature);
                PlayerState side = Game.State.Player(battle.PlayerOf(stack.side));
                title.text = $"{stack.count} {(stack.count == 1 ? def.Name : def.Plural)}";
                detail.text = side == null ? "A wandering army moves."
                    : side.index == manager.Viewer ? "Your troops are moving."
                    : $"{side.name} is moving.";
                UIKit.Enable(wait, false);
                UIKit.Enable(defend, false);
                UIKit.Enable(cast, false);
                UIKit.Enable(retreat, false);
                Order(battle);
                shown = -1;
            }
        }

        /// <summary>The stack in hand is this device's: work out where it may go and what it may strike.</summary>
        public void Show(BattleState battle)
        {
            root.gameObject.SetActive(true);
            BattleStack stack = battle.Current;
            if (stack == null)
            {
                return;
            }
            if (shown != stack.id)
            {
                shown = stack.id;
                Casting = SpellId.None;
                Compute(stack);
            }
            CreatureDef def = Creatures.Get(stack.creature);
            title.text = $"{stack.count} {(stack.count == 1 ? def.Name : def.Plural)}";
            detail.text = $"Attack {def.Attack}  Defense {def.Defense}  Damage {def.MinDamage}-{def.MaxDamage}\n" +
                          $"Speed {def.Speed}  Health {stack.health}/{def.Health}" +
                          (def.Shots > 0 ? $"  Shots {stack.shots}" : "");
            Order(battle);

            bool ours = !manager.HeroesUI.Busy;
            UIKit.Enable(wait, ours && !stack.waited);
            UIKit.Enable(defend, ours);
            HeroState hero = Game.State.Hero(battle.HeroOf(stack.side));
            UIKit.Enable(cast, ours && hero != null && hero.spells.Count > 0 && !battle.HasCast(stack.side));
            UIKit.Enable(retreat, ours && hero != null);
            manager.Battle.Highlight(battle, reach.Keys, def.Shots > 0 && stack.shots > 0 ? shots : targets, def.Shots > 0 && stack.shots > 0);
        }

        /// <summary>The cells the stack can step onto, the enemies it can strike, and the ones it can shoot.</summary>
        private void Compute(BattleStack stack)
        {
            BattleState battle = State;
            reach = Game.BattleReach(stack);
            targets.Clear();
            shots.Clear();
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
        }

        private void Order(BattleState battle)
        {
            for (int i = order.childCount - 1; i >= 0; i--)
            {
                Destroy(order.GetChild(i).gameObject);
            }
            int shownCount = 0;
            foreach (int id in battle.order)
            {
                BattleStack stack = battle.Stack(id);
                if (stack == null || !stack.alive || shownCount >= 16)
                {
                    continue;
                }
                shownCount++;
                CreatureDef def = Creatures.Get(stack.creature);
                Image chip = UIKit.Slot(order, def.Name);
                UIKit.Fit((RectTransform)chip.transform, 46f, 46f);
                chip.color = stack.id == battle.current
                    ? new Color(1f, 0.92f, 0.6f)
                    : HeroesArt.PlayerColor(battle.PlayerOf(stack.side)) * 0.8f;
                TextMeshProUGUI count = UIKit.Label(chip.transform, "Count", stack.count.ToString(), 15f, UIKit.Ink,
                    TextAlignmentOptions.Center);
                UIKit.Stretch((RectTransform)count.transform, 2f, 2f, 2f, 2f);
                Tooltip.Attach(chip.gameObject, $"{stack.count} {def.Plural}");
            }
        }

        // ------------------------------------------------------------------ the pointer on the field

        /// <summary>Paints what a click on <paramref name="cell"/> would do, and does it when the click comes.</summary>
        public void Hover(int cell)
        {
            BattleState battle = State;
            if (battle == null || !battle.active || manager.HeroesUI.Busy)
            {
                return;
            }
            hovered = cell;
            if (cell >= 0 && Input.GetMouseButtonDown(0) && !manager.Rig.IsDragging)
            {
                Click(cell);
            }
            if (Input.GetMouseButtonDown(1) && Casting != SpellId.None)
            {
                Casting = SpellId.None;
                Show(battle);
            }
        }

        private void Click(int cell)
        {
            BattleState battle = State;
            BattleStack stack = battle.Current;
            if (stack == null)
            {
                return;
            }
            if (Casting != SpellId.None)
            {
                manager.Commands.BattleCast(Casting, cell);
                Casting = SpellId.None;
                return;
            }
            BattleStack target = battle.StackAt(cell);
            CreatureDef def = Creatures.Get(stack.creature);
            if (target != null && target.alive && target.side != stack.side)
            {
                if (def.Shots > 0 && stack.shots > 0 && Game.CanShoot(stack))
                {
                    manager.Commands.BattleShoot(stack.id, cell);
                    return;
                }
                List<int> from = Game.AttackCells(stack, target, reach);
                if (from.Count > 0)
                {
                    manager.Commands.BattleAttack(stack.id, cell, Nearest(from));
                }
                return;
            }
            if (reach.ContainsKey(cell))
            {
                manager.Commands.BattleMove(stack.id, cell);
            }
        }

        /// <summary>Of the cells a blow could be struck from, the one nearest the pointer.</summary>
        private int Nearest(List<int> cells)
        {
            if (cells.Count == 1 || manager.Map == null)
            {
                return cells[0];
            }
            Vector3 point = manager.Rig.PointerGround(Input.mousePosition, out Vector3 ground, manager.Map.GroundMask)
                ? ground
                : manager.Map.Point(cells[0]);
            int best = cells[0];
            float closest = float.MaxValue;
            foreach (int cell in cells)
            {
                float distance = (manager.Map.Point(cell) - point).sqrMagnitude;
                if (distance < closest)
                {
                    closest = distance;
                    best = cell;
                }
            }
            return best;
        }

        // ------------------------------------------------------------------ the buttons

        private void Wait()
        {
            BattleStack stack = State?.Current;
            if (stack != null)
            {
                manager.Commands.BattleWait(stack.id);
            }
        }

        private void Defend()
        {
            BattleStack stack = State?.Current;
            if (stack != null)
            {
                manager.Commands.BattleDefend(stack.id);
            }
        }

        private void Retreat()
        {
            manager.Commands.BattleRetreat();
        }

        private void OpenSpells()
        {
            BattleState battle = State;
            BattleStack stack = battle?.Current;
            HeroState hero = stack != null ? Game.State.Hero(battle.HeroOf(stack.side)) : null;
            if (hero != null)
            {
                spellBox.Show(hero);
            }
        }

        public void Pick(SpellId spell)
        {
            Casting = spell;
            SpellDef def = Spells.Get(spell);
            detail.text = def != null ? $"{def.Name}: click a cell to cast, right click to think better of it." : "";
        }

        public void Finish(BattleResult result, BattleState battle)
        {
            Hide();
        }
    }

    /// <summary>The spellbook as it is opened in a battle: what the hero knows, and what he can still pay for.</summary>
    public sealed class SpellBox : Dialog
    {
        private BattleBar bar;

        public static SpellBox Make(Transform parent, HeroesGameManager manager, BattleBar owner)
        {
            SpellBox box = Build<SpellBox>(parent, manager, "Spellbook", new Vector2(900f, 560f), "Spellbook");
            box.bar = owner;
            UIKit.Grid(box.Body, new Vector2(196f, 92f), new Vector2(10f, 10f), 4);
            return box;
        }

        public void Show(HeroState hero)
        {
            ClearBody();
            ClearButtons();
            Heading.text = $"{hero.Name} — {hero.mana} mana";
            foreach (int id in hero.spells)
            {
                var spell = (SpellId)id;
                SpellDef def = Spells.Get(spell);
                if (def == null)
                {
                    continue;
                }
                bool can = hero.mana >= def.Cost && def.Level <= Manager.Game.SpellLevelLimit(hero);
                Image card = UIKit.Panel(Body, def.Name, can ? 0.9f : 0.5f);
                var button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = card;
                button.colors = UIKit.Tint();
                button.interactable = can;
                button.onClick.AddListener(() =>
                {
                    bar.Pick(spell);
                    Close();
                });
                card.gameObject.AddComponent<Clicker>();
                Image icon = UIKit.Sprite(card.transform, "Icon", Manager.Art.Spell(spell), Color.white);
                UIKit.Pin((RectTransform)icon.transform, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(58f, 58f));
                ((RectTransform)icon.transform).pivot = new Vector2(0f, 0.5f);
                TextMeshProUGUI name = UIKit.Label(card.transform, "Name", def.Name, 19f, can ? UIKit.Ink : UIKit.Dim);
                UIKit.Pin((RectTransform)name.transform, new Vector2(0f, 1f), new Vector2(76f, -14f), new Vector2(110f, 44f));
                TextMeshProUGUI cost = UIKit.Label(card.transform, "Cost", $"{def.Cost} mana", 16f, UIKit.Gold);
                UIKit.Pin((RectTransform)cost.transform, new Vector2(0f, 0f), new Vector2(76f, 12f), new Vector2(110f, 22f));
                Tooltip.Attach(card.gameObject, $"{def.Name} (level {def.Level})\n{def.Description}");
            }
            Answer("Close", Close);
            Open();
        }
    }
}
