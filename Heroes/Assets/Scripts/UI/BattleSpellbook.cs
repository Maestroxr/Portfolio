using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// The spellbook as it is opened in a battle: every spell the hero knows, what it costs and whether he can pay for
    /// it and cast it now. Picking one closes the book and leaves the battle bar waiting for a target.
    /// </summary>
    public sealed class BattleSpellbook : MonoBehaviour
    {
        private const float Width = 1000f;
        private const float Height = 640f;

        private HeroesGameManager manager;
        private RectTransform grid;
        private TextMeshProUGUI heading;
        private TextMeshProUGUI hint;
        private Action<SpellId> picked;

        public bool IsOpen => gameObject.activeSelf;

        public static BattleSpellbook Make(RectTransform parent, HeroesGameManager manager)
        {
            RectTransform holder = UIKit.Rect(parent, "Battle Spellbook");
            UIKit.Stretch(holder);
            var book = holder.gameObject.AddComponent<BattleSpellbook>();
            book.manager = manager;
            Image shade = UIKit.Shade(holder, "Shade", 0.6f);
            shade.raycastTarget = true;
            Button behind = shade.gameObject.AddComponent<Button>();
            behind.transition = Selectable.Transition.None;
            behind.onClick.AddListener(book.Close);

            Image frame = UIKit.Frame(holder, "Window");
            var window = (RectTransform)frame.transform;
            UIKit.Pin(window, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(Width, Height));
            RectTransform content = UIKit.Content(frame, 16f);
            Image ribbon = UIKit.Ribbon(window, "Title", "Spellbook", 36f);
            UIKit.Pin((RectTransform)ribbon.transform, new Vector2(0.5f, 1f), new Vector2(0f, 40f), new Vector2(480f, 96f));

            book.heading = UIKit.Label(content, "Hero", "", 25f, UIKit.Ink, TextAlignmentOptions.Center);
            UIKit.Pin((RectTransform)book.heading.transform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(Width - 100f, 34f));

            book.grid = UIKit.Rect(content, "Spells");
            UIKit.Stretch(book.grid, 12f, 92f, 12f, 88f);
            GridLayoutGroup layout = UIKit.Grid(book.grid, new Vector2(214f, 96f), new Vector2(12f, 12f), 4);
            layout.childAlignment = TextAnchor.UpperCenter;

            book.hint = UIKit.Label(content, "Hint", "", 20f, UIKit.Dim, TextAlignmentOptions.Center);
            book.hint.fontStyle = FontStyles.Italic;
            UIKit.Pin((RectTransform)book.hint.transform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(Width - 100f, 28f));
            Button close = UIKit.Push(content, "Close", "Close", book.Close, 26f);
            UIKit.Pin((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(240f, 56f));
            holder.gameObject.SetActive(false);
            return book;
        }

        /// <summary>Opens the book of the hero of <paramref name="side"/>; <paramref name="pick"/> gets the spell chosen.</summary>
        public void Show(HeroState hero, int side, Action<SpellId> pick)
        {
            picked = pick;
            HeroesGame game = manager.Game;
            for (int i = grid.childCount - 1; i >= 0; i--)
            {
                Destroy(grid.GetChild(i).gameObject);
            }
            int most = game != null ? game.MaxMana(hero) : hero.mana;
            heading.text = $"{hero.Name}   <sprite name=\"mana\"> {hero.mana} / {most} spell points";
            string reason = null;
            foreach (int id in hero.spells)
            {
                var spell = (SpellId)id;
                SpellDef def = Spells.Get(spell);
                if (def == null)
                {
                    continue;
                }
                string cannot = game != null ? game.CannotCast(side, spell) : "No battle.";
                if (cannot == null && game != null && def.Level > game.SpellLevelLimit(hero))
                {
                    cannot = "Beyond the hero's wisdom.";
                }
                reason = reason ?? cannot;
                Card(spell, def, cannot);
            }
            hint.text = hero.spells.Count == 0 ? "The hero knows no spells." :
                reason == "One spell a round." ? "A hero casts one spell a round." : "Pick a spell, then its target on the field.";
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        private void Card(SpellId spell, SpellDef def, string cannot)
        {
            bool can = cannot == null;
            Button button = UIKit.CardButton(grid, def.Name, () =>
            {
                Action<SpellId> pick = picked;
                Close();
                pick?.Invoke(spell);
            });
            button.interactable = can;
            Transform card = button.transform;
            Image icon = UIKit.Sprite(card, "Icon", UIKit.Art != null ? UIKit.Art.Spell(spell) : null, can ? Color.white : new Color(0.5f, 0.5f, 0.5f));
            UIKit.Pin((RectTransform)icon.transform, new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(62f, 62f));
            TextMeshProUGUI name = UIKit.Label(card, "Name", def.Name, 21f, can ? UIKit.Ink : UIKit.Dim, TextAlignmentOptions.TopLeft);
            // One line, shrunk to fit: a name on two lines would run into the cost under it.
            UIKit.FitLine(name, 21f, 14f);
            UIKit.Pin((RectTransform)name.transform, new Vector2(0f, 1f), new Vector2(84f, -14f), new Vector2(120f, 34f));
            TextMeshProUGUI cost = UIKit.Label(card, "Cost", $"<sprite name=\"mana\"> {def.Cost}", 19f, can ? UIKit.Gold : UIKit.Dim, TextAlignmentOptions.BottomLeft);
            UIKit.Pin((RectTransform)cost.transform, new Vector2(0f, 0f), new Vector2(84f, 12f), new Vector2(120f, 26f));
            Tooltip.Attach(button.gameObject, $"{def.Name}, level {def.Level}\n{def.Description}{(can ? "" : $"\n<color=#E08070>{cannot}</color>")}");
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
