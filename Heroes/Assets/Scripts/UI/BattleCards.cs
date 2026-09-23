using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// The numbers of a stack as a battle shows them: its creature's own, with what its hero and the spells on it add.
    /// The copy of the battle the view keeps (<see cref="BattleView.Shown"/>) carries the counts, cells and spells.
    /// </summary>
    public static class BattleNumbers
    {
        public static int Attack(HeroesGame game, BattleState battle, BattleStack stack)
        {
            HeroState hero = game.State.Hero(battle.HeroOf(stack.side));
            int attack = stack.Def.Attack + (hero != null ? game.Stat(hero, PrimaryStat.Attack) : 0);
            return attack + stack.EffectAmount(SpellId.Bloodlust) - stack.EffectAmount(SpellId.Weakness) + stack.EffectAmount(SpellId.Prayer);
        }

        public static int Defense(HeroesGame game, BattleState battle, BattleStack stack)
        {
            HeroState hero = game.State.Hero(battle.HeroOf(stack.side));
            int defense = stack.Def.Defense + (hero != null ? game.Stat(hero, PrimaryStat.Defense) : 0);
            return defense + stack.EffectAmount(SpellId.StoneSkin) + stack.EffectAmount(SpellId.Prayer);
        }

        /// <summary>The health of one creature of the stack, with its hero's bonus.</summary>
        public static int Health(HeroesGame game, BattleState battle, BattleStack stack)
        {
            HeroState hero = game.State.Hero(battle.HeroOf(stack.side));
            return stack.Def.Health + game.ArmyHealthBonus(hero);
        }

        public static int Speed(HeroesGame game, BattleState battle, BattleStack stack)
        {
            if (stack.Def.Has(Ability.Immobile))
            {
                return 0;
            }
            HeroState hero = game.State.Hero(battle.HeroOf(stack.side));
            int speed = stack.Def.Speed + game.ArmySpeedBonus(hero) + stack.EffectAmount(SpellId.Haste) + stack.EffectAmount(SpellId.Prayer);
            if (stack.HasEffect(SpellId.Slow))
            {
                speed /= 2;
            }
            return Mathf.Max(1, speed);
        }

        /// <summary>The name of a stack with its count: "12 Swordsmen", "an Arrow Tower".</summary>
        public static string Troop(BattleStack stack)
        {
            CreatureDef def = stack.Def;
            return stack.IsTower ? def.Name : $"{stack.count} {def.NameFor(stack.count)}";
        }

        /// <summary>What a creature can do beyond walking and striking, in a few words.</summary>
        public static string Abilities(CreatureDef def)
        {
            var parts = new List<string>();
            if (def.IsFlying) parts.Add("flies");
            if (def.IsRanged) parts.Add("shoots");
            if (def.Has(Ability.DoubleAttack)) parts.Add("strikes twice");
            if (def.Has(Ability.NoRetaliation)) parts.Add("no retaliation");
            if (def.Has(Ability.UnlimitedRetaliation)) parts.Add("retaliates without end");
            if (def.Has(Ability.TwoRetaliations)) parts.Add("retaliates twice");
            if (def.Has(Ability.LifeDrain)) parts.Add("drains life");
            if (def.Has(Ability.AreaShot)) parts.Add("shots burst");
            if (def.Has(Ability.Charge)) parts.Add("charges");
            if (def.Has(Ability.Breath)) parts.Add("breath");
            if (def.Has(Ability.Regenerate)) parts.Add("regenerates");
            if (def.Has(Ability.MagicResist)) parts.Add("resists magic");
            if (def.Has(Ability.Fearsome)) parts.Add("fearsome");
            if (def.IsUndead) parts.Add("undead");
            if (parts.Count == 0)
            {
                return "";
            }
            var text = new StringBuilder(parts[0].Substring(0, 1).ToUpperInvariant()).Append(parts[0].Substring(1));
            for (int i = 1; i < parts.Count; i++)
            {
                text.Append(", ").Append(parts[i]);
            }
            return text.Append('.').ToString();
        }
    }

    /// <summary>
    /// A hero at an end of the battle bar: his portrait in its frame with the pennant of his colors, his name and class,
    /// and the spell points he has left. A wandering army or a town without a hero shows its own face instead.
    /// </summary>
    public sealed class BattleHeroBlock : MonoBehaviour
    {
        private Image portrait;
        private Image pennant;
        private TextMeshProUGUI title;
        private TextMeshProUGUI detail;
        private Image mana;
        private TextMeshProUGUI manaText;
        private RectTransform manaRow;

        public static BattleHeroBlock Make(RectTransform parent, bool left)
        {
            RectTransform rect = UIKit.Rect(parent, left ? "Attacker" : "Defender");
            float x = left ? 0f : 1f;
            rect.anchorMin = new Vector2(x, 0f);
            rect.anchorMax = new Vector2(x, 1f);
            rect.pivot = new Vector2(x, 0.5f);
            rect.sizeDelta = new Vector2(400f, 0f);
            rect.anchoredPosition = Vector2.zero;
            var block = rect.gameObject.AddComponent<BattleHeroBlock>();
            float sign = left ? 1f : -1f;
            Vector2 corner = new Vector2(x, 0.5f);

            block.pennant = UIKit.Pennant(rect, "Pennant", Color.white);
            UIKit.Pin((RectTransform)block.pennant.transform, new Vector2(x, 1f), new Vector2(sign * 16f, 4f), new Vector2(34f, 84f));

            block.portrait = UIKit.PortraitFrame(rect, "Portrait", null);
            var frame = (RectTransform)block.portrait.transform.parent.parent;
            UIKit.Pin(frame, corner, new Vector2(sign * 44f, 0f), new Vector2(128f, 128f));

            float textX = sign * 188f;
            TextAlignmentOptions align = left ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;
            block.title = UIKit.Heading(rect, "Name", "", 27f, align);
            // A long name shrinks to fit; an ellipsis would hide the whole line in a rect this short.
            UIKit.FitLine(block.title, 27f, 18f);
            UIKit.Pin((RectTransform)block.title.transform, corner, new Vector2(textX, 38f), new Vector2(206f, 36f));
            block.detail = UIKit.Label(rect, "Class", "", 20f, UIKit.Dim, align);
            block.detail.textWrappingMode = TextWrappingModes.NoWrap;
            UIKit.Pin((RectTransform)block.detail.transform, corner, new Vector2(textX, 6f), new Vector2(206f, 28f));

            block.manaRow = UIKit.Rect(rect, "Mana");
            UIKit.Pin(block.manaRow, corner, new Vector2(textX, -34f), new Vector2(206f, 30f));
            block.mana = UIKit.Meter(block.manaRow, "Meter", new Color(0.35f, 0.6f, 1f));
            var meter = (RectTransform)block.mana.transform.parent;
            UIKit.Pin(meter, new Vector2(x, 0.5f), Vector2.zero, new Vector2(206f, 22f));
            block.manaText = UIKit.Label(block.manaRow, "Points", "", 18f, Color.white, TextAlignmentOptions.Center);
            block.manaText.fontStyle = FontStyles.Bold;
            UIKit.Look(block.manaText, TextLook.Outline);
            UIKit.Stretch((RectTransform)block.manaText.transform, 0f, -4f, 0f, -4f);
            return block;
        }

        /// <summary>Shows the side <paramref name="side"/> of <paramref name="battle"/>.</summary>
        public void Show(HeroesGame game, BattleState battle, int side, int color)
        {
            HeroesArt art = UIKit.Art;
            pennant.color = HeroesArt.PlayerColor(color);
            HeroState hero = game.State.Hero(battle.HeroOf(side));
            if (hero != null)
            {
                HeroClass heroClass = hero.Def != null ? hero.Def.Class : HeroClass.Knight;
                Picture(portrait, art != null ? art.HeroPortrait(heroClass) : null);
                title.text = hero.Name;
                detail.text = $"{HeroData.Class(heroClass).Name}, level {hero.level}";
                int most = Mathf.Max(1, game.MaxMana(hero));
                mana.fillAmount = Mathf.Clamp01(hero.mana / (float)most);
                manaText.text = $"<sprite name=\"mana\"> {hero.mana} / {most}";
                manaRow.gameObject.SetActive(true);
                return;
            }
            manaRow.gameObject.SetActive(false);
            TownState town = side == 1 ? game.State.Town(battle.town) : null;
            if (town != null)
            {
                Picture(portrait, art != null ? art.TownPortrait(town.faction, color) : null);
                title.text = town.name;
                detail.text = "The garrison";
                return;
            }
            BattleStack leader = null;
            foreach (BattleStack stack in battle.stacks)
            {
                if (stack.side == side && !stack.IsTower && (leader == null || stack.Def.Tier > leader.Def.Tier))
                {
                    leader = stack;
                }
            }
            Picture(portrait, leader != null && art != null ? art.Portrait((CreatureId)leader.creature) : null);
            title.text = leader != null ? leader.Def.Plural : "The wilds";
            detail.text = "A wandering army";
        }

        /// <summary>Puts a picture into a portrait frame; with none the frame stays empty rather than showing white.</summary>
        public static void Picture(Image portrait, Sprite sprite)
        {
            portrait.sprite = sprite;
            portrait.enabled = sprite != null;
        }
    }

    /// <summary>
    /// A stack's card: its portrait, how many there are, its numbers (attack, defense, damage, health, speed, shots) and
    /// the spells on it. The battle bar keeps one for the stack whose turn it is; a right click on any stack shows a
    /// larger one with what the creature can do.
    /// </summary>
    public sealed class BattleStackCard : MonoBehaviour
    {
        private Image portrait;
        private Image plate;
        private TextMeshProUGUI title;
        private TextMeshProUGUI numbers;
        private TextMeshProUGUI note;
        private RectTransform effects;
        private readonly List<Image> effectIcons = new List<Image>();
        private bool large;

        /// <summary>A card on <paramref name="parent"/>: the bar's compact one, or the <paramref name="large"/> one of a right click.</summary>
        public static BattleStackCard Make(Transform parent, string name, bool large)
        {
            Image card = large ? UIKit.Frame(parent, name) : UIKit.Card(parent, name);
            var rect = (RectTransform)card.transform;
            var view = card.gameObject.AddComponent<BattleStackCard>();
            view.large = large;
            RectTransform content = UIKit.Content(card, large ? 10f : 4f);
            float face = large ? 132f : 84f;

            view.portrait = UIKit.PortraitFrame(content, "Portrait", null);
            var frame = (RectTransform)view.portrait.transform.parent.parent;
            UIKit.Pin(frame, new Vector2(0f, 1f), new Vector2(0f, large ? -2f : 0f), new Vector2(face, face));
            view.plate = UIKit.Sprite(frame, "Side", UIKit.Art != null ? UIKit.Art.bar : null, Color.white);
            UIKit.Pin((RectTransform)view.plate.transform, new Vector2(0.5f, 0f), new Vector2(0f, -3f), new Vector2(face - 18f, 7f));

            float left = face + (large ? 18f : 12f);
            view.title = UIKit.Heading(content, "Name", "", large ? 30f : 24f, TextAlignmentOptions.TopLeft);
            view.title.textWrappingMode = TextWrappingModes.NoWrap;
            var titleRect = (RectTransform)view.title.transform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.offsetMin = new Vector2(left, large ? -40f : -30f);
            titleRect.offsetMax = new Vector2(large ? 0f : -120f, 0f);

            view.numbers = UIKit.Label(content, "Numbers", "", large ? 23f : 20f, UIKit.Ink, TextAlignmentOptions.TopLeft);
            var numbersRect = (RectTransform)view.numbers.transform;
            numbersRect.anchorMin = new Vector2(0f, 0f);
            numbersRect.anchorMax = new Vector2(1f, 1f);
            numbersRect.offsetMin = new Vector2(left, 0f);
            numbersRect.offsetMax = new Vector2(0f, large ? -46f : -34f);
            view.numbers.lineSpacing = large ? 12f : 0f;

            if (large)
            {
                view.note = UIKit.Label(content, "Note", "", 20f, UIKit.Dim, TextAlignmentOptions.TopLeft);
                view.note.fontStyle = FontStyles.Italic;
                var noteRect = (RectTransform)view.note.transform;
                noteRect.anchorMin = new Vector2(0f, 0f);
                noteRect.anchorMax = new Vector2(1f, 0f);
                noteRect.pivot = new Vector2(0f, 0f);
                noteRect.offsetMin = new Vector2(0f, 2f);
                noteRect.offsetMax = new Vector2(0f, 58f);
            }

            view.effects = UIKit.Rect(content, "Spells");
            if (large)
            {
                UIKit.Pin(view.effects, new Vector2(0f, 1f), new Vector2(0f, -face - 12f), new Vector2(face, 34f));
            }
            else
            {
                UIKit.Pin(view.effects, new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(116f, 30f));
            }
            HorizontalLayoutGroup row = UIKit.Layout<HorizontalLayoutGroup>(view.effects, 4f);
            row.childAlignment = large ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            row.childControlWidth = false;
            row.childControlHeight = false;
            return view;
        }

        public void Show(HeroesGame game, BattleState battle, BattleStack stack, int color)
        {
            CreatureDef def = stack.Def;
            BattleHeroBlock.Picture(portrait, UIKit.Art != null ? UIKit.Art.Portrait((CreatureId)stack.creature) : null);
            plate.color = HeroesArt.PlayerColor(color);
            title.text = BattleNumbers.Troop(stack);
            int health = BattleNumbers.Health(game, battle, stack);
            string attack = $"<sprite name=\"attack\"> {BattleNumbers.Attack(game, battle, stack)}";
            string defense = $"<sprite name=\"defense\"> {BattleNumbers.Defense(game, battle, stack)}";
            string damage = $"<sprite name=\"damage\"> {def.MinDamage}-{def.MaxDamage}";
            string life = $"<sprite name=\"health\"> {(stack.alive ? stack.health : 0)}/{health}";
            string speed = $"<sprite name=\"speed\"> {BattleNumbers.Speed(game, battle, stack)}";
            string shots = def.Shots > 0 ? $"<color=#E8D8A8>Shots {stack.shots}</color>" : "";
            if (large)
            {
                numbers.text = $"Attack  {attack}    Defense  {defense}\nDamage  {damage}    Health  {life}\nSpeed  {speed}" +
                               (def.Shots > 0 ? $"    {shots}" : "");
                note.text = BattleNumbers.Abilities(def);
                // The card is as tall as what it has to say: the spells on the stack and what its creature can do.
                bool more = !string.IsNullOrEmpty(note.text) || stack.effects.Count > 0;
                var rect = (RectTransform)transform;
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, more ? 330f : 262f);
            }
            else
            {
                numbers.text = $"{attack}   {defense}   {damage}   {life}   {speed}" + (def.Shots > 0 ? $"   {shots}" : "");
            }
            ShowEffects(stack);
        }

        private void ShowEffects(BattleStack stack)
        {
            HeroesArt art = UIKit.Art;
            while (effectIcons.Count < stack.effects.Count)
            {
                Image icon = UIKit.Sprite(effects, "Spell", null, Color.white);
                ((RectTransform)icon.transform).sizeDelta = new Vector2(large ? 32f : 28f, large ? 32f : 28f);
                icon.raycastTarget = true;
                effectIcons.Add(icon);
            }
            for (int i = 0; i < effectIcons.Count; i++)
            {
                bool on = i < stack.effects.Count;
                effectIcons[i].gameObject.SetActive(on);
                if (!on)
                {
                    continue;
                }
                EffectState effect = stack.effects[i];
                var spell = (SpellId)effect.spell;
                SpellDef def = Spells.Get(spell);
                effectIcons[i].sprite = art != null ? art.Spell(spell) : null;
                effectIcons[i].color = def != null && !def.IsPositive ? new Color(1f, 0.75f, 0.85f) : Color.white;
                Tooltip.Attach(effectIcons[i].gameObject,
                    $"{(def != null ? def.Name : "A spell")}: {effect.rounds} round{(effect.rounds == 1 ? "" : "s")} left");
            }
        }
    }

    /// <summary>A stack waiting its turn, in the row along the top of the battle bar: its portrait, its count, its side.</summary>
    public sealed class BattleQueueChip : MonoBehaviour
    {
        private Image portrait;
        private Image plate;
        private TextMeshProUGUI count;
        private RectTransform frame;
        private Image halo;

        public int Stack { get; private set; } = -1;

        public static BattleQueueChip Make(Transform parent)
        {
            RectTransform rect = UIKit.Rect(parent, "Chip");
            UIKit.Fit(rect, 72f, 86f);
            var chip = rect.gameObject.AddComponent<BattleQueueChip>();
            // The stack whose turn it is stands on a lit plate, a size bigger than the rest.
            HeroesArt art = UIKit.Art;
            Sprite lit = art != null ? art.slotSelected != null ? art.slotSelected : art.slot : null;
            chip.halo = UIKit.Sprite(rect, "Turn", lit, new Color(1f, 0.9f, 0.55f, 1f));
            chip.halo.type = Image.Type.Sliced;
            chip.halo.pixelsPerUnitMultiplier = 2f;
            UIKit.Pin((RectTransform)chip.halo.transform, new Vector2(0.5f, 1f), new Vector2(0f, 6f), new Vector2(86f, 104f));
            chip.halo.gameObject.SetActive(false);
            chip.portrait = UIKit.PortraitFrame(rect, "Portrait", null);
            chip.frame = (RectTransform)chip.portrait.transform.parent.parent;
            UIKit.Pin(chip.frame, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(70f, 70f));
            chip.plate = UIKit.Sprite(rect, "Count", UIKit.Art != null ? UIKit.Art.bar : null, Color.white);
            UIKit.Pin((RectTransform)chip.plate.transform, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(54f, 24f));
            chip.count = UIKit.Label(chip.plate.transform, "Number", "", 18f, UIKit.Ink, TextAlignmentOptions.Center);
            chip.count.fontStyle = FontStyles.Bold;
            chip.count.textWrappingMode = TextWrappingModes.NoWrap;
            UIKit.Stretch((RectTransform)chip.count.transform);
            chip.portrait.transform.parent.GetComponent<Image>().raycastTarget = true;
            return chip;
        }

        /// <summary>
        /// Shows a stack: its portrait, count and side; one of the <paramref name="nextRound"/> is greyed, and the stack
        /// <paramref name="acting"/> now is lit and stands a little proud of the rest.
        /// </summary>
        public void Show(BattleStack stack, int color, bool nextRound, bool acting)
        {
            Stack = stack.id;
            halo.gameObject.SetActive(acting);
            frame.localScale = Vector3.one * (acting ? 1.1f : 1f);
            BattleHeroBlock.Picture(portrait, UIKit.Art != null ? UIKit.Art.Portrait((CreatureId)stack.creature) : null);
            Color side = HeroesArt.PlayerColor(color);
            plate.color = new Color(side.r * 0.75f, side.g * 0.75f, side.b * 0.75f, 1f);
            count.text = stack.IsTower ? "" : stack.count.ToString();
            portrait.color = nextRound ? new Color(0.65f, 0.65f, 0.65f) : Color.white;
            Tooltip.Attach(portrait.transform.parent.gameObject, $"{BattleNumbers.Troop(stack)}{(nextRound ? " (next round)" : "")}");
        }
    }

    /// <summary>
    /// The small box by the pointer over the field: what a click there does, and for a blow, a shot or a spell the damage
    /// it would deal and the creatures it would kill.
    /// </summary>
    public sealed class BattlePointerTip : MonoBehaviour
    {
        private RectTransform box;
        private TextMeshProUGUI label;
        private RectTransform canvas;

        /// <summary>Where the pointer is, in screen pixels: the box stands by it.</summary>
        public Vector3 At { get; set; }

        public static BattlePointerTip Make(RectTransform parent)
        {
            Image frame = UIKit.Art != null && UIKit.Art.tooltip != null
                ? UIKit.Sprite(parent, "Pointer Tip", UIKit.Art.tooltip, Color.white)
                : UIKit.Card(parent, "Pointer Tip");
            frame.raycastTarget = false;
            var tip = frame.gameObject.AddComponent<BattlePointerTip>();
            tip.canvas = parent;
            tip.box = (RectTransform)frame.transform;
            tip.box.anchorMin = tip.box.anchorMax = Vector2.zero;
            tip.box.pivot = Vector2.zero;
            RectOffset padding = UIKit.Padding(frame, 4f);
            padding.left = Mathf.Max(padding.left, 16);
            padding.right = Mathf.Max(padding.right, 16);
            padding.top = Mathf.Max(padding.top, 10);
            padding.bottom = Mathf.Max(padding.bottom, 10);
            VerticalLayoutGroup layout = UIKit.Layout<VerticalLayoutGroup>(tip.box, 0f, padding);
            layout.childForceExpandWidth = true;
            tip.label = UIKit.Label(tip.box, "Text", "", 21f, UIKit.Ink);
            tip.label.textWrappingMode = TextWrappingModes.NoWrap;
            var fitter = tip.box.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            tip.gameObject.SetActive(false);
            return tip;
        }

        public void Show(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                Hide();
                return;
            }
            if (label.text != text)
            {
                label.text = text;
            }
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            box.SetAsLastSibling();
            Place();
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            Place();
        }

        private void Place()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, At, null, out Vector2 point);
            Rect area = canvas.rect;
            Vector2 size = box.rect.size;
            float x = point.x - area.xMin + 26f;
            float y = point.y - area.yMin + 22f;
            if (x + size.x > area.width - 8f)
            {
                x = point.x - area.xMin - 26f - size.x;
            }
            if (y + size.y > area.height - 8f)
            {
                y = point.y - area.yMin - 22f - size.y;
            }
            box.anchoredPosition = new Vector2(x, y);
        }
    }
}
