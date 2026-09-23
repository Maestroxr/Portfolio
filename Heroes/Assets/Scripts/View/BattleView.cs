using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes
{
    /// <summary>
    /// A battle as it is fought on the map itself: the hexagons of the field are drawn over the ground the armies met
    /// on, every stack stands on its cell with the count of its creatures over it, and the blows, shots and spells of
    /// the rules are played out one event at a time.
    /// </summary>
    public sealed class BattleView : MonoBehaviour
    {
        private static readonly Color MoveColor = new Color(0.45f, 0.75f, 1f, 0.3f);
        private static readonly Color ShootColor = new Color(1f, 0.85f, 0.35f, 0.3f);
        private static readonly Color StrikeColor = new Color(1f, 0.35f, 0.3f, 0.38f);
        private static readonly Color TurnColor = new Color(1f, 0.92f, 0.55f, 0.45f);

        private MapView map;
        private HeroesArt art;
        private Effects effects;
        private HeroesSettings settings;
        private readonly Dictionary<int, UnitView> stacks = new Dictionary<int, UnitView>();
        private readonly Dictionary<int, Badge> badges = new Dictionary<int, Badge>();
        private Transform root;

        public bool Running { get; private set; }

        /// <summary>How fast the battle is played out, from the settings.</summary>
        public float Speed => settings != null ? settings.battleSpeed : 1f;

        public void Setup(MapView view, HeroesArt catalog, Effects show, HeroesSettings options)
        {
            map = view;
            art = catalog;
            effects = show;
            settings = options;
        }

        public UnitView Stack(int id)
        {
            return stacks.TryGetValue(id, out UnitView view) ? view : null;
        }

        // ------------------------------------------------------------------ setting up and clearing away

        /// <summary>Draws the field and puts every stack on it.</summary>
        public void Begin(BattleState battle)
        {
            Clear();
            Running = true;
            root = new GameObject("Battle").transform;
            root.SetParent(transform, false);

            float radius = (BattleState.HalfWidth + 1.5f) * map.Layout.CellWidth;
            map.Grid.ShowBattle(battle.cells, map.Point(battle.center), radius);

            foreach (BattleStack stack in battle.stacks)
            {
                if (stack.alive)
                {
                    Add(battle, stack);
                }
            }
        }

        private void Add(BattleState battle, BattleStack stack)
        {
            var view = new GameObject($"Stack {stack.id}").AddComponent<UnitView>();
            view.transform.SetParent(root, false);
            view.transform.position = map.Point(stack.cell);
            view.Setup(art, (CreatureId)stack.creature);
            view.Cell = stack.cell;
            view.Face(Toward(battle, stack.side));
            stacks[stack.id] = view;

            var badge = Badge.Make(view.transform, art, HeroesArt.PlayerColor(battle.PlayerOf(stack.side)));
            badge.Set(stack.count);
            badges[stack.id] = badge;
        }

        /// <summary>The way a side faces: the attacker looks along the field, the defender back down it.</summary>
        private Vector3 Toward(BattleState battle, int side)
        {
            Vector3 left = map.Point(battle.cells.Count > 0 ? battle.cells[0] : battle.center);
            Vector3 center = map.Point(battle.center);
            Vector3 along = (center - left).normalized;
            if (along.sqrMagnitude < 0.0001f)
            {
                along = Vector3.right;
            }
            bool facingRight = side == 0 == battle.attackerLeft;
            return facingRight ? along : -along;
        }

        public void End()
        {
            Running = false;
            map.Grid.HideBattle();
            map.Grid.ClearPaint();
            Clear();
        }

        private void Clear()
        {
            stacks.Clear();
            badges.Clear();
            if (root != null)
            {
                Destroy(root.gameObject);
                root = null;
            }
        }

        // ------------------------------------------------------------------ showing what can be done

        /// <summary>Paints the cells the stack whose turn it is may step onto and the ones it may strike.</summary>
        public void Highlight(BattleState battle, IEnumerable<int> reach, IEnumerable<int> targets, bool shooting)
        {
            map.Grid.ClearPaint();
            if (reach != null)
            {
                map.Grid.Paint(reach, MoveColor);
            }
            if (targets != null)
            {
                map.Grid.Paint(targets, shooting ? ShootColor : StrikeColor);
            }
            BattleStack current = battle.Current;
            if (current != null && current.alive)
            {
                map.Grid.Paint(current.cell, TurnColor);
            }
        }

        public void ClearHighlight()
        {
            map.Grid.ClearPaint();
        }

        // ------------------------------------------------------------------ playing the events out

        /// <summary>Plays one event of the battle. Events the view has nothing to show for pass in no time.</summary>
        public IEnumerator Play(GameEvent what, BattleState battle)
        {
            switch (what.kind)
            {
                case EventKind.StackTurn:
                    yield return Turn(battle, what.a);
                    break;
                case EventKind.StackMoved:
                    yield return Moved(what);
                    break;
                case EventKind.StackAttacked:
                    yield return Struck(battle, what, false);
                    break;
                case EventKind.StackShot:
                    yield return Struck(battle, what, true);
                    break;
                case EventKind.StackDamaged:
                    yield return Damaged(what.a, what.b, what.c);
                    break;
                case EventKind.StackHealed:
                    Healed(what.a, what.b, what.c);
                    break;
                case EventKind.StackDied:
                    yield return Died(what.a);
                    break;
                case EventKind.StackDefended:
                    Say(what.a, "Defend", new Color(0.7f, 0.85f, 1f));
                    break;
                case EventKind.StackWaited:
                    Say(what.a, "Wait", new Color(0.85f, 0.85f, 0.85f));
                    break;
                case EventKind.MoraleBoost:
                    Say(what.a, "Morale!", new Color(0.5f, 1f, 0.6f));
                    yield return Wait(0.35f);
                    break;
                case EventKind.MoraleFail:
                    Say(what.a, "Wavers", new Color(0.8f, 0.5f, 0.4f));
                    yield return Wait(0.35f);
                    break;
                case EventKind.LuckyStrike:
                    Say(what.a, "Lucky!", new Color(1f, 0.92f, 0.4f));
                    break;
                case EventKind.SpellCast:
                    yield return Spell(battle, what);
                    break;
                case EventKind.EffectAdded:
                    Aura(what.a, Spells.Get((SpellId)what.b));
                    break;
                case EventKind.CreaturesRaised:
                    Raised(battle, what);
                    break;
            }
        }

        private IEnumerator Wait(float seconds)
        {
            yield return new WaitForSeconds(seconds / Mathf.Max(0.2f, Speed));
        }

        private IEnumerator Turn(BattleState battle, int stackId)
        {
            UnitView view = Stack(stackId);
            if (view == null)
            {
                yield break;
            }
            effects.Aura(view.transform.position + Vector3.up * 0.05f, TurnColor);
            yield return Wait(0.1f);
        }

        private IEnumerator Moved(GameEvent what)
        {
            UnitView view = Stack(what.a);
            if (view == null)
            {
                yield break;
            }
            bool flying = what.d != 0;
            IReadOnlyList<int> path = what.cells;
            float speed = 3.2f * Speed;
            if (flying || path == null || path.Count == 0)
            {
                // A flier goes over everything in the way, straight to where it lands.
                Vector3 to = map.Point(what.c);
                yield return view.Turn(to - view.transform.position);
                view.Puppet.Walk();
                Vector3 from = view.transform.position;
                float t = 0f;
                float length = Mathf.Max(0.35f, Vector3.Distance(from, to) / speed);
                while (t < 1f)
                {
                    t += Time.deltaTime / length;
                    Vector3 at = Vector3.Lerp(from, to, Mathf.Clamp01(t));
                    at.y += Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * (flying ? 1.6f : 0.25f);
                    view.transform.position = at;
                    yield return null;
                }
                view.transform.position = to;
                view.Puppet.Idle();
            }
            else
            {
                foreach (int cell in path)
                {
                    yield return view.WalkTo(map.Point(cell), speed);
                }
            }
            view.Cell = what.c;
        }

        private IEnumerator Struck(BattleState battle, GameEvent what, bool ranged)
        {
            UnitView attacker = Stack(what.a);
            UnitView target = Stack(what.b);
            if (attacker == null || target == null)
            {
                yield break;
            }
            Vector3 at = target.Middle;
            yield return attacker.Turn(target.transform.position - attacker.transform.position);
            if (ranged)
            {
                string clip = string.IsNullOrEmpty(attacker.ShootClip) ? attacker.AttackClip : attacker.ShootClip;
                if (attacker.Puppet.Has(clip))
                {
                    attacker.Puppet.Play(clip);
                    yield return Wait(0.25f);
                }
                yield return effects.Shoot(attacker.Projectile, attacker.Middle, at);
            }
            else
            {
                yield return attacker.Perform(attacker.AttackClip, target.transform.position);
            }
            yield return Damaged(what.b, what.c, what.d);
        }

        private IEnumerator Damaged(int stackId, int damage, int killed)
        {
            UnitView view = Stack(stackId);
            if (view == null)
            {
                yield break;
            }
            effects.Float(view.Middle, damage > 0 ? $"-{damage}" : "0", new Color(1f, 0.55f, 0.45f));
            if (killed > 0)
            {
                effects.Burst(view.Middle, new Color(0.7f, 0.15f, 0.12f), 18, 2.4f);
            }
            if (view.Puppet.Has(view.HitClip))
            {
                view.Puppet.Play(view.HitClip);
            }
            else
            {
                view.Puppet.Flinch();
            }
            if (badges.TryGetValue(stackId, out Badge badge))
            {
                badge.Add(-killed);
            }
            yield return Wait(0.22f);
        }

        private void Healed(int stackId, int healed, int raised)
        {
            UnitView view = Stack(stackId);
            if (view == null)
            {
                return;
            }
            effects.Float(view.Middle, raised > 0 ? $"+{raised}" : $"+{healed}", new Color(0.5f, 1f, 0.6f));
            effects.Rise(view.Middle, new Color(0.5f, 1f, 0.65f));
            if (raised > 0 && badges.TryGetValue(stackId, out Badge badge))
            {
                badge.Add(raised);
            }
        }

        private IEnumerator Died(int stackId)
        {
            UnitView view = Stack(stackId);
            if (view == null)
            {
                yield break;
            }
            if (badges.TryGetValue(stackId, out Badge badge))
            {
                badge.Hide();
                badges.Remove(stackId);
            }
            float length = view.Puppet.Die(view.DeathClip);
            stacks.Remove(stackId);
            StartCoroutine(Fade(view.gameObject, length + 0.8f));
            yield return Wait(0.3f);
        }

        private IEnumerator Fade(GameObject what, float after)
        {
            yield return new WaitForSeconds(after);
            if (what != null)
            {
                Destroy(what);
            }
        }

        private IEnumerator Spell(BattleState battle, GameEvent what)
        {
            var spell = (SpellId)what.b;
            SpellDef def = Spells.Get(spell);
            Vector3 at = map.Point(what.c, 0.5f);
            effects.Float(at + Vector3.up * 1.2f, def != null ? def.Name : "Spell", new Color(0.7f, 0.8f, 1f));
            switch (spell)
            {
                case SpellId.LightningBolt:
                    yield return effects.Lightning(at);
                    break;
                case SpellId.Fireball:
                    yield return effects.Explosion(at, map.Layout.CellWidth * 1.2f, new Color(1f, 0.6f, 0.2f));
                    break;
                case SpellId.MeteorShower:
                    yield return effects.Meteors(at, map.Layout.CellWidth * 1.5f);
                    break;
                default:
                    effects.Aura(at, new Color(0.65f, 0.75f, 1f));
                    yield return Wait(0.3f);
                    break;
            }
        }

        private void Aura(int stackId, SpellDef def)
        {
            UnitView view = Stack(stackId);
            if (view == null)
            {
                return;
            }
            Color color = def != null && !def.IsPositive ? new Color(0.8f, 0.4f, 0.9f) : new Color(0.5f, 0.85f, 1f);
            effects.Aura(view.transform.position + Vector3.up * 0.05f, color);
        }

        private void Raised(BattleState battle, GameEvent what)
        {
            UnitView view = Stack(what.a);
            if (view == null)
            {
                return;
            }
            effects.Rise(view.Middle, new Color(0.6f, 1f, 0.7f));
            if (badges.TryGetValue(what.a, out Badge badge))
            {
                badge.Add(what.c);
            }
        }

        private void Say(int stackId, string text, Color color)
        {
            UnitView view = Stack(stackId);
            if (view != null)
            {
                effects.Float(view.Middle, text, color, 2.6f);
            }
        }

        // ------------------------------------------------------------------ the count over a stack

        /// <summary>The little plate under a stack that says how many creatures are left in it.</summary>
        private sealed class Badge : MonoBehaviour
        {
            private TextMeshProUGUI label;
            private int count;
            private Camera view;

            public static Badge Make(Transform owner, HeroesArt art, Color color)
            {
                var holder = new GameObject("Count");
                holder.transform.SetParent(owner, false);
                holder.transform.localPosition = new Vector3(0f, 0.1f, -0.55f);
                var canvas = holder.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                var rect = (RectTransform)canvas.transform;
                rect.sizeDelta = new Vector2(64f, 32f);
                rect.localScale = Vector3.one * 0.012f;

                var plate = new GameObject("Plate", typeof(Image));
                plate.transform.SetParent(rect, false);
                var plateRect = (RectTransform)plate.transform;
                plateRect.anchorMin = Vector2.zero;
                plateRect.anchorMax = Vector2.one;
                plateRect.offsetMin = Vector2.zero;
                plateRect.offsetMax = Vector2.zero;
                Image image = plate.GetComponent<Image>();
                image.sprite = art.panel;
                image.type = Image.Type.Sliced;
                image.color = new Color(color.r * 0.55f, color.g * 0.55f, color.b * 0.55f, 0.95f);

                var text = new GameObject("Text", typeof(TextMeshProUGUI));
                text.transform.SetParent(rect, false);
                var textRect = (RectTransform)text.transform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                var label = text.GetComponent<TextMeshProUGUI>();
                label.font = art.bodyFont;
                label.fontSize = 22f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.white;
                label.enableAutoSizing = false;

                var badge = holder.AddComponent<Badge>();
                badge.label = label;
                badge.view = Camera.main;
                return badge;
            }

            public void Set(int value)
            {
                count = Mathf.Max(0, value);
                label.text = count.ToString();
            }

            public void Add(int change)
            {
                Set(count + change);
            }

            public void Hide()
            {
                gameObject.SetActive(false);
            }

            private void LateUpdate()
            {
                if (view == null)
                {
                    view = Camera.main;
                    if (view == null)
                    {
                        return;
                    }
                }
                // The plate always turns to the player, whichever way the creature is looking.
                transform.rotation = view.transform.rotation;
            }
        }
    }
}
