using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// How a battle went, when it is over: victory, defeat or retreat for the player at this device, the two sides with
    /// the creatures each lost, and the experience the winner gained. Its button takes the battle off the screen.
    /// Everything comes from the copy of the battle as it ended (<see cref="GameEvent.battle"/> of BattleEnded).
    /// </summary>
    public sealed class BattleResults : MonoBehaviour
    {
        private const float Width = 980f;
        private const float Height = 600f;

        private RectTransform window;
        private Image ribbon;
        private TextMeshProUGUI ribbonText;
        private TextMeshProUGUI verdict;
        private readonly Image[] portraits = new Image[2];
        private readonly TextMeshProUGUI[] names = new TextMeshProUGUI[2];
        private readonly RectTransform[] losses = new RectTransform[2];
        private TextMeshProUGUI experience;
        private Action closed;
        private CanvasGroup group;
        private float shownAt;

        public bool IsOpen => gameObject.activeSelf;

        public static BattleResults Make(RectTransform parent)
        {
            RectTransform holder = UIKit.Rect(parent, "Battle Results");
            UIKit.Stretch(holder);
            var results = holder.gameObject.AddComponent<BattleResults>();
            results.group = holder.gameObject.AddComponent<CanvasGroup>();
            Image shade = UIKit.Shade(holder, "Shade", 0.72f);
            shade.raycastTarget = true;

            Image frame = UIKit.Frame(holder, "Window");
            results.window = (RectTransform)frame.transform;
            UIKit.Pin(results.window, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(Width, Height));
            RectTransform content = UIKit.Content(frame, 18f);

            results.ribbon = UIKit.Ribbon(results.window, "Title", "Victory", 40f);
            UIKit.Pin((RectTransform)results.ribbon.transform, new Vector2(0.5f, 1f), new Vector2(0f, 42f), new Vector2(560f, 104f));
            results.ribbonText = results.ribbon.GetComponentInChildren<TextMeshProUGUI>();

            results.verdict = UIKit.Label(content, "Verdict", "", 25f, UIKit.Ink, TextAlignmentOptions.Center);
            results.verdict.fontStyle = FontStyles.Italic;
            UIKit.Pin((RectTransform)results.verdict.transform, new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(Width - 120f, 40f));

            for (int side = 0; side < 2; side++)
            {
                results.Side(content, side);
            }
            TextMeshProUGUI versus = UIKit.Heading(content, "Versus", "vs", 34f);
            UIKit.Pin((RectTransform)versus.transform, new Vector2(0.5f, 1f), new Vector2(0f, -146f), new Vector2(80f, 44f));

            RectTransform rule = UIKit.Divider(content, "Rule", 20f);
            UIKit.Pin(rule, new Vector2(0.5f, 0f), new Vector2(0f, 128f), new Vector2(Width - 160f, 20f));
            results.experience = UIKit.Label(content, "Experience", "", 24f, UIKit.Gold, TextAlignmentOptions.Center);
            UIKit.Pin((RectTransform)results.experience.transform, new Vector2(0.5f, 0f), new Vector2(0f, 84f), new Vector2(Width - 140f, 36f));

            Button button = UIKit.Push(content, "Continue", "Continue", results.Close, 28f);
            UIKit.Pin((RectTransform)button.transform, new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(300f, 64f));
            holder.gameObject.SetActive(false);
            return results;
        }

        /// <summary>One side: the portrait of its hero (or of its army), its name, and the creatures it lost.</summary>
        private void Side(RectTransform content, int side)
        {
            float x = side == 0 ? -Width * 0.25f + 8f : Width * 0.25f - 8f;
            portraits[side] = UIKit.PortraitFrame(content, side == 0 ? "Attacker" : "Defender", null);
            var frame = (RectTransform)portraits[side].transform.parent.parent;
            UIKit.Pin(frame, new Vector2(0.5f, 1f), new Vector2(x, -96f), new Vector2(116f, 116f));
            names[side] = UIKit.Heading(content, "Name", "", 26f);
            names[side].textWrappingMode = TextWrappingModes.NoWrap;
            UIKit.Pin((RectTransform)names[side].transform, new Vector2(0.5f, 1f), new Vector2(x, -218f), new Vector2(Width * 0.46f, 36f));
            TextMeshProUGUI caption = UIKit.Label(content, "Caption", "Casualties", 20f, UIKit.Dim, TextAlignmentOptions.Center);
            UIKit.Pin((RectTransform)caption.transform, new Vector2(0.5f, 1f), new Vector2(x, -254f), new Vector2(Width * 0.46f, 28f));
            losses[side] = UIKit.Rect(content, "Losses");
            UIKit.Pin(losses[side], new Vector2(0.5f, 1f), new Vector2(x, -288f), new Vector2(Width * 0.46f, 96f));
            HorizontalLayoutGroup row = UIKit.Layout<HorizontalLayoutGroup>(losses[side], 8f);
            row.childAlignment = TextAnchor.UpperCenter;
            row.childControlWidth = false;
            row.childControlHeight = false;
        }

        /// <summary>
        /// Shows how <paramref name="end"/> (the battle as it ended) went for <paramref name="mySide"/>, the side of the
        /// player at this device; <paramref name="done"/> is called when the player moves on.
        /// </summary>
        public void Show(GameEvent ended, BattleState end, int mySide, GameState state, Action done)
        {
            closed = done;
            var result = (BattleResult)ended.a;
            bool attackerWon = result == BattleResult.AttackerWon || result == BattleResult.DefenderFled;
            bool won = attackerWon ? mySide == 0 : mySide == 1;
            bool fled = result == BattleResult.AttackerFled && mySide == 0 || result == BattleResult.DefenderFled && mySide == 1;
            ribbonText.text = fled ? "Retreat" : won ? "Victory" : "Defeat";
            ribbon.color = won ? Color.white : new Color(0.62f, 0.62f, 0.7f);
            verdict.text = Verdict(ended, end, result, state);
            for (int side = 0; side < 2; side++)
            {
                ShowSide(end, side, state);
            }
            HeroState winner = state.Hero(end.HeroOf(attackerWon ? 0 : 1));
            int gained = (attackerWon ? end.defenderLostHealth : end.attackerLostHealth) + (end.town >= 0 && attackerWon ? 500 : 0);
            experience.text = winner != null && winner.alive && gained > 0
                ? $"<sprite name=\"experience\"> {winner.Name} gains {gained} experience."
                : "";
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            shownAt = Time.unscaledTime;
            group.alpha = 0f;
        }

        private static string Verdict(GameEvent ended, BattleState end, BattleResult result, GameState state)
        {
            if (!string.IsNullOrEmpty(ended.text))
            {
                return ended.text;
            }
            string attacker = Name(end, 0, state, out bool attackers);
            string defender = Name(end, 1, state, out bool defenders);
            string line;
            switch (result)
            {
                case BattleResult.AttackerWon:
                    line = $"{attacker} {(attackers ? "carry" : "carries")} the field against {defender}.";
                    break;
                case BattleResult.DefenderWon:
                    line = $"{defender} {(defenders ? "stand" : "stands")} firm against {attacker}.";
                    break;
                case BattleResult.AttackerFled:
                    line = $"{attacker} {(attackers ? "withdraw" : "withdraws")} from the field.";
                    break;
                default:
                    line = $"{defender} {(defenders ? "withdraw" : "withdraws")} from the field.";
                    break;
            }
            return char.ToUpperInvariant(line[0]) + line.Substring(1);
        }

        /// <summary>What a side is called in a sentence, and whether it takes a plural verb (a band of creatures does).</summary>
        private static string Name(BattleState end, int side, GameState state, out bool plural)
        {
            plural = false;
            HeroState hero = state.Hero(end.HeroOf(side));
            if (hero != null)
            {
                return hero.Name;
            }
            TownState town = side == 1 ? state.Town(end.town) : null;
            if (town != null)
            {
                return $"the garrison of {town.name}";
            }
            foreach (BattleStack stack in end.stacks)
            {
                if (stack.side == side && !stack.IsTower)
                {
                    plural = true;
                    return $"the {stack.Def.Plural}";
                }
            }
            return "the wilds";
        }

        private void ShowSide(BattleState end, int side, GameState state)
        {
            HeroesArt art = UIKit.Art;
            HeroState hero = state.Hero(end.HeroOf(side));
            TownState town = side == 1 ? state.Town(end.town) : null;
            PlayerState player = state.Player(end.PlayerOf(side));
            int color = player != null ? (int)player.color : 4;
            if (hero != null)
            {
                BattleHeroBlock.Picture(portraits[side], art != null ? art.HeroPortrait(hero.Def != null ? hero.Def.Class : HeroClass.Knight) : null);
                names[side].text = hero.Name;
            }
            else if (town != null)
            {
                BattleHeroBlock.Picture(portraits[side], art != null ? art.TownPortrait(town.faction, color) : null);
                names[side].text = town.name;
            }
            else
            {
                BattleStack leader = null;
                foreach (BattleStack stack in end.stacks)
                {
                    if (stack.side == side && !stack.IsTower && (leader == null || stack.Def.Tier > leader.Def.Tier))
                    {
                        leader = stack;
                    }
                }
                BattleHeroBlock.Picture(portraits[side], leader != null && art != null ? art.Portrait((CreatureId)leader.creature) : null);
                names[side].text = leader != null ? $"Wandering {leader.Def.Plural}" : "The wilds";
            }
            names[side].color = Color.Lerp(Color.white, HeroesArt.PlayerColor(color), 0.35f);

            RectTransform row = losses[side];
            for (int i = row.childCount - 1; i >= 0; i--)
            {
                Destroy(row.GetChild(i).gameObject);
            }
            List<ArmySlot> lost = side == 0 ? end.attackerLosses : end.defenderLosses;
            if (lost == null || lost.Count == 0)
            {
                TextMeshProUGUI none = UIKit.Label(row, "None", "None", 22f, UIKit.Dim, TextAlignmentOptions.Center);
                ((RectTransform)none.transform).sizeDelta = new Vector2(200f, 40f);
                return;
            }
            int shown = 0;
            foreach (ArmySlot slot in lost)
            {
                if (slot.count <= 0 || shown >= 6)
                {
                    continue;
                }
                shown++;
                RectTransform chip = UIKit.Rect(row, "Loss");
                chip.sizeDelta = new Vector2(68f, 92f);
                Image face = UIKit.PortraitFrame(chip, "Portrait", art != null ? art.Portrait((CreatureId)slot.creature) : null);
                UIKit.Pin((RectTransform)face.transform.parent.parent, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(66f, 66f));
                CreatureDef def = Creatures.Get(slot.creature);
                Tooltip.Attach(face.transform.parent.gameObject, def != null ? $"{slot.count} {def.NameFor(slot.count)}" : "");
                TextMeshProUGUI count = UIKit.Label(chip, "Count", slot.count.ToString(), 21f, UIKit.Ink, TextAlignmentOptions.Center);
                count.fontStyle = FontStyles.Bold;
                UIKit.Pin((RectTransform)count.transform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(68f, 24f));
            }
        }

        public void Close()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }
            gameObject.SetActive(false);
            Action done = closed;
            closed = null;
            done?.Invoke();
        }

        /// <summary>Takes the dialog away without moving on: the battle it tells of is being left altogether.</summary>
        public void Dismiss()
        {
            closed = null;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            float t = Mathf.Clamp01((Time.unscaledTime - shownAt) / 0.35f);
            group.alpha = t;
            window.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, 1f - (1f - t) * (1f - t));
            if (t >= 1f && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)))
            {
                Close();
            }
        }
    }
}
