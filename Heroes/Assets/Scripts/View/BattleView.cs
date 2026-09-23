using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes
{
    /// <summary>
    /// A battle as it is seen, in either style: on the hexagons of the map (<see cref="MapBattlefield"/>) or on a
    /// battlefield of its own (<see cref="BattlefieldScene"/>), which it knows only through <see cref="IBattlefield"/>.
    /// It stands every stack on its cell with the count of its creatures in front of it and the two heroes at the ends
    /// of the field, then plays the blows, shots and spells of the rules out one event at a time. It keeps a copy of the
    /// battle as it has shown it so far (<see cref="Shown"/>), built from the copy the rules hand over when the battle
    /// starts and changed by nothing but the events, so what is on the screen never runs ahead of what was played,
    /// however far ahead the rules are (online they may be several moves further).
    /// </summary>
    public sealed class BattleView : MonoBehaviour
    {
        public static readonly Color MoveColor = new Color(0.55f, 0.82f, 1f, 0.26f);
        public static readonly Color ShootColor = new Color(1f, 0.82f, 0.3f, 0.34f);
        public static readonly Color StrikeColor = new Color(1f, 0.32f, 0.26f, 0.36f);
        public static readonly Color TurnColor = new Color(1f, 0.9f, 0.45f, 0.5f);
        public static readonly Color SpellColor = new Color(0.72f, 0.55f, 1f, 0.34f);
        public static readonly Color HoverColor = new Color(1f, 0.97f, 0.85f, 0.95f);

        private IBattlefield field;
        private HeroesArt art;
        private Effects effects;
        private HeroesSettings settings;
        private HeroesAudio sound;
        private HeroesGame game;
        private readonly Dictionary<int, UnitView> stacks = new Dictionary<int, UnitView>();
        private readonly Dictionary<int, Badge> badges = new Dictionary<int, Badge>();
        private readonly HeroView[] heroes = new HeroView[2];
        private Transform root;
        private RectTransform counts;

        /// <summary>Whether a battle is on the screen (from its start to its end).</summary>
        public bool Running { get; private set; }

        /// <summary>The field the battle is shown on, while it is.</summary>
        public IBattlefield Field => field;

        /// <summary>The battle as far as it has been shown: the copy of its start, with every event played since.</summary>
        public BattleState Shown { get; private set; }

        /// <summary>A line for the combat log for an event just played, with the side it is about (-1 for none).</summary>
        public event Action<string, int> Told;

        /// <summary>How fast the battle is played out, from the settings.</summary>
        public float Speed => settings != null ? Mathf.Max(0.25f, settings.battleSpeed) : 1f;

        public void Setup(HeroesArt catalog, Effects show, HeroesSettings options, HeroesAudio audio)
        {
            art = catalog;
            effects = show;
            settings = options;
            sound = audio;
        }

        public UnitView Stack(int id)
        {
            return stacks.TryGetValue(id, out UnitView view) ? view : null;
        }

        /// <summary>The hero (or banner) of a side on the field, or null.</summary>
        public HeroView Hero(int side)
        {
            return side == 0 || side == 1 ? heroes[side] : null;
        }

        // ------------------------------------------------------------------ setting up and clearing away

        /// <summary>
        /// Shows a battle from <paramref name="start"/> (the copy of it as it was deployed) on <paramref name="on"/>:
        /// the grid, every stack on its cell, and the heroes of both sides. <paramref name="rules"/> is the game, for the
        /// heroes, the colors of the players and the health of a creature; it is only read.
        /// </summary>
        public void Begin(BattleState start, IBattlefield on, HeroesGame rules)
        {
            Clear();
            field = on;
            game = rules;
            Shown = start.Clone();
            Running = true;
            if (effects != null)
            {
                effects.Speed = Speed;
                effects.View = field.Camera;
                // A battlefield of its own is seen from farther off than the map: its numbers are drawn larger still.
                effects.TextScale = field.UnitScale > 1f ? field.UnitScale * 1.35f : 1f;
            }
            root = new GameObject("Battle").transform;
            root.SetParent(field.Root, false);
            field.ShowGrid();
            foreach (BattleStack stack in Shown.stacks)
            {
                if (stack.alive)
                {
                    Add(stack);
                }
            }
            for (int side = 0; side < 2; side++)
            {
                heroes[side] = HeroOnField(side);
            }
        }

        /// <summary>The color of a side, as a number: its player's, or the grey of nobody for the wilds.</summary>
        public int SideColor(int side)
        {
            PlayerState player = World != null && Shown != null ? World.Player(Shown.PlayerOf(side)) : null;
            return player != null ? (int)player.color : 4;
        }

        private void Add(BattleStack stack)
        {
            var view = new GameObject($"Stack {stack.id}").AddComponent<UnitView>();
            view.transform.SetParent(root, false);
            view.transform.position = field.Point(stack.cell);
            // An arrow tower flies the colors of the town it defends, and stands as still as the stones it is made of.
            GameObject look = stack.IsTower && art != null ? art.Tower(SideColor(stack.side)) : null;
            view.Setup(art, (CreatureId)stack.creature, look, stack.IsTower);
            view.transform.localScale = Vector3.one * Size(view, stack.IsTower);
            view.Cell = stack.cell;
            view.Face(field.Facing((HexSide)stack.facing));
            stacks[stack.id] = view;
            if (!stack.IsTower)
            {
                Badge badge = Badge.Make(view.transform, Counts(), art, HeroesArt.PlayerColor(SideColor(stack.side)), field.Camera,
                    stack.side == 0 ? 1f : -1f, 1f + (field.UnitScale - 1f) * 0.6f);
                badge.Set(stack.count);
                badges[stack.id] = badge;
            }
        }

        /// <summary>
        /// The layer the count plates are drawn on: a canvas over the whole screen, under the interface (the battle bar,
        /// its dialogs and the fade into a battlefield are on the interface's canvas, of order 10) and over the field.
        /// </summary>
        private RectTransform Counts()
        {
            if (counts == null)
            {
                var layer = new GameObject("Battle Counts", typeof(RectTransform), typeof(Canvas));
                layer.transform.SetParent(transform, false);
                var canvas = layer.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 5;
                counts = (RectTransform)layer.transform;
            }
            return counts;
        }

        /// <summary>
        /// How big a stack is drawn on the field: as much bigger as the field shows its troops, but no creature much
        /// wider than its cell, so the big ones do not spill far over their neighbours (an arrow tower grows a little
        /// less, to stand well over the town's wall without crowding the cells beside it).
        /// </summary>
        private float Size(UnitView view, bool tower)
        {
            float scale = field.UnitScale;
            if (tower || scale <= 1f)
            {
                return tower ? 1f + (scale - 1f) * 0.4f : scale;
            }
            // Measured as it stands, not spread out as it was modelled.
            view.Puppet?.Pose();
            float wide = Footprint(view.gameObject);
            return wide > 0.01f ? Mathf.Clamp(field.CellWidth * 1.3f / wide, 1f, scale) : scale;
        }

        /// <summary>
        /// How wide the body of a model stands on the ground (the larger of its width and depth), in meters: what it
        /// crowds its neighbours with (a sword or a spear may reach over into the next cell, as in the old games). The
        /// bounds of a skinned renderer are those of every pose of its animation, often many times the creature itself,
        /// so its bones are measured; a model without bones by its meshes.
        /// </summary>
        private static float Footprint(GameObject model)
        {
            float left = float.MaxValue, right = float.MinValue, near = float.MaxValue, far = float.MinValue;
            void Take(Vector3 point)
            {
                left = Mathf.Min(left, point.x);
                right = Mathf.Max(right, point.x);
                near = Mathf.Min(near, point.z);
                far = Mathf.Max(far, point.z);
            }
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    foreach (Transform bone in skinned.bones)
                    {
                        if (bone != null)
                        {
                            Take(bone.position);
                        }
                    }
                }
            }
            if (left <= right)
            {
                // The flesh around the bones, and past the last joints (finger and wing tips, snouts, tails).
                return Mathf.Max(right - left, far - near) * 1.25f + 0.3f;
            }
            foreach (Renderer renderer in renderers)
            {
                if (renderer is MeshRenderer && renderer.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
                {
                    Bounds local = filter.sharedMesh.bounds;
                    for (int corner = 0; corner < 8; corner++)
                    {
                        var sign = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                        Take(renderer.transform.TransformPoint(local.center + Vector3.Scale(local.extents, sign)));
                    }
                }
            }
            return left > right ? 0f : Mathf.Max(right - left, far - near);
        }

        /// <summary>The hero of a side at his end of the field (none for a wandering army or a town without one).</summary>
        private HeroView HeroOnField(int side)
        {
            field.HeroSpot(side, out Vector3 spot, out Vector3 facing);
            HeroState hero = World != null ? World.Hero(Shown.HeroOf(side)) : null;
            if (hero != null)
            {
                var view = new GameObject($"Hero {hero.Name}").AddComponent<HeroView>();
                view.transform.SetParent(root, false);
                view.transform.position = spot;
                view.transform.localScale = Vector3.one * field.UnitScale;
                view.Setup(art, hero, SideColor(side));
                view.Face(facing);
                return view;
            }
            // A wandering army has no leader to stand there, and a town under siege is its own (the field shows it).
            return null;
        }

        public void End()
        {
            Running = false;
            if (effects != null)
            {
                effects.TextScale = 1f;
            }
            if (field != null)
            {
                field.HideGrid();
                field.ClearPaint();
                field.Mark(-1, Color.clear);
            }
            Clear();
            field = null;
        }

        private void Clear()
        {
            StopAllCoroutines();
            stacks.Clear();
            // The plates are on the layer over the field, not under the battle's root.
            foreach (Badge badge in badges.Values)
            {
                if (badge != null)
                {
                    badge.Remove();
                }
            }
            badges.Clear();
            heroes[0] = heroes[1] = null;
            if (root != null)
            {
                Destroy(root.gameObject);
                root = null;
            }
        }

        // ------------------------------------------------------------------ showing what can be done

        /// <summary>
        /// Paints the cells the stack in hand may step onto, the ones it may strike (or shoot, or cast on), and its own.
        /// </summary>
        public void Highlight(IEnumerable<int> reach, IEnumerable<int> targets, Color targetColor, int current)
        {
            if (field == null)
            {
                return;
            }
            field.ClearPaint();
            if (reach != null)
            {
                foreach (int cell in reach)
                {
                    field.Paint(cell, MoveColor);
                }
            }
            if (targets != null)
            {
                foreach (int cell in targets)
                {
                    field.Paint(cell, targetColor);
                }
            }
            if (current >= 0)
            {
                field.Paint(current, TurnColor);
            }
        }

        public void ClearHighlight()
        {
            field?.ClearPaint();
            field?.Mark(-1, Color.clear);
        }

        /// <summary>Rings the cell under the pointer (-1: none).</summary>
        public void Hover(int cell, Color color)
        {
            field?.Mark(cell, color);
        }

        /// <summary>
        /// The cell of the stack whose body is under <paramref name="screen"/> (the one nearest the camera where bodies
        /// overlap), or -1. A creature stands up from its cell, so the pointer on its chest or its head is over the ground
        /// of the cells behind it: what the player points at is the creature, not that ground. <paramref name="ground"/>
        /// stands for where on the body the pointer is, on the level of its cell, for the side a blow comes from: at its
        /// feet the middle of the cell, at its head the side away from the camera, and the side of the body the pointer
        /// is on (below its feet the ground of its cell is under the pointer, which gives the side toward the camera).
        /// </summary>
        public int StackUnder(Vector3 screen, out Vector3 ground)
        {
            ground = default;
            Camera view = field != null ? field.Camera : null;
            if (view == null || Shown == null)
            {
                return -1;
            }
            var pointer = new Vector2(screen.x, screen.y);
            int best = -1;
            float nearest = float.MaxValue;
            float bestAcross = 0f, bestAlong = 0f;
            foreach (KeyValuePair<int, UnitView> pair in stacks)
            {
                UnitView unit = pair.Value;
                BattleStack stack = Shown.Stack(pair.Key);
                if (unit == null || !unit.gameObject.activeInHierarchy || stack == null || !stack.alive)
                {
                    continue;
                }
                // From its feet (a flier's hang in the air over its cell) to the top of its head.
                Vector3 feet = unit.transform.position + Vector3.up * (unit.Flies ? unit.Height * 0.35f : 0f);
                Vector3 head = feet + Vector3.up * unit.Height;
                Vector3 bottom = view.WorldToScreenPoint(feet);
                Vector3 top = view.WorldToScreenPoint(head);
                if (bottom.z <= 0f || top.z <= 0f)
                {
                    continue;
                }
                // About as wide as the creature: most of its cell for a big one, less for a slender one.
                float half = Mathf.Clamp(unit.Height * 0.3f, field.CellWidth * 0.25f, field.CellWidth * 0.42f);
                Vector3 middle = view.WorldToScreenPoint((feet + head) * 0.5f);
                Vector3 edge = view.WorldToScreenPoint((feet + head) * 0.5f + view.transform.right * half);
                float width = Mathf.Max(2f, new Vector2(edge.x - middle.x, edge.y - middle.y).magnitude);
                var from = new Vector2(bottom.x, bottom.y);
                Vector2 up = new Vector2(top.x, top.y) - from;
                float length = up.sqrMagnitude;
                if (length < 1f)
                {
                    continue;
                }
                // Below its feet is the ground in front of it, which the field reads on its own.
                float along = Vector2.Dot(pointer - from, up) / length;
                if (along < 0f || along > 1.05f)
                {
                    continue;
                }
                float across = (pointer - (from + up * Mathf.Min(1f, along))).x / width;
                if (Mathf.Abs(across) > 1f || middle.z >= nearest)
                {
                    continue;
                }
                nearest = middle.z;
                best = unit.Cell;
                bestAcross = across;
                bestAlong = Mathf.Clamp01(along);
            }
            if (best < 0)
            {
                return -1;
            }
            Vector3 right = view.transform.right;
            right.y = 0f;
            Vector3 away = view.transform.forward;
            away.y = 0f;
            right = right.sqrMagnitude > 1e-6f ? right.normalized : Vector3.right;
            away = away.sqrMagnitude > 1e-6f ? away.normalized : Vector3.forward;
            ground = field.Point(best) + (right * bestAcross + away * bestAlong) * (field.CellWidth * 0.5f);
            return best;
        }

        // ------------------------------------------------------------------ playing the events out

        /// <summary>
        /// Plays one event of the battle: the copy of the battle takes it first, the combat log is told of it, then it is
        /// shown. Events the view has nothing to show for pass in no time.
        /// </summary>
        public IEnumerator Play(GameEvent what)
        {
            if (!Running || Shown == null)
            {
                yield break;
            }
            string told = Describe(what, out int side);
            Apply(what);
            if (!string.IsNullOrEmpty(told))
            {
                Told?.Invoke(told, side);
            }
            switch (what.kind)
            {
                case EventKind.StackTurn:
                    yield return Turn(what.a);
                    break;
                case EventKind.StackMoved:
                    yield return Moved(what);
                    break;
                case EventKind.StackAttacked:
                    yield return Struck(what, false);
                    break;
                case EventKind.StackShot:
                    yield return Struck(what, true);
                    break;
                case EventKind.StackDamaged:
                    yield return Damaged(what.a, what.b, what.c, what.d >= 0);
                    break;
                case EventKind.StackHealed:
                    yield return Healed(what);
                    break;
                case EventKind.StackDied:
                    yield return Died(what.a);
                    break;
                case EventKind.StackDefended:
                    Say(what.a, "Defend", new Color(0.7f, 0.85f, 1f));
                    Aura(what.a, new Color(0.55f, 0.75f, 1f));
                    yield return Wait(0.25f);
                    break;
                case EventKind.StackWaited:
                    Say(what.a, "Wait", new Color(0.85f, 0.85f, 0.85f));
                    yield return Wait(0.2f);
                    break;
                case EventKind.MoraleBoost:
                    sound?.Play(Sfx.Buff, 0.7f);
                    Say(what.a, "Morale!", new Color(0.5f, 1f, 0.6f));
                    Aura(what.a, new Color(0.5f, 1f, 0.6f));
                    yield return Wait(0.45f);
                    break;
                case EventKind.MoraleFail:
                    sound?.Play(Sfx.Curse, 0.6f);
                    Say(what.a, "Wavers", new Color(0.8f, 0.5f, 0.4f));
                    yield return Wait(0.45f);
                    break;
                case EventKind.LuckyStrike:
                    sound?.Play(Sfx.Buff, 0.6f);
                    Say(what.a, "Lucky!", new Color(1f, 0.92f, 0.4f));
                    break;
                case EventKind.SpellCast:
                    yield return Spell(what);
                    break;
                case EventKind.EffectAdded:
                    SpellDef def = Spells.Get((SpellId)what.b);
                    Aura(what.a, def != null && !def.IsPositive ? new Color(0.8f, 0.4f, 0.9f) : new Color(0.5f, 0.85f, 1f));
                    yield return Wait(0.12f);
                    break;
            }
        }

        /// <summary>The copy of the battle takes an event, as the rules took the command behind it.</summary>
        private void Apply(GameEvent what)
        {
            BattleState battle = Shown;
            BattleStack stack = battle.Stack(what.a);
            switch (what.kind)
            {
                case EventKind.RoundBegan:
                    battle.round = what.a;
                    foreach (BattleStack each in battle.stacks)
                    {
                        each.acted = false;
                        each.waited = false;
                        each.retaliations = 0;
                        if (what.a > 1)
                        {
                            for (int i = each.effects.Count - 1; i >= 0; i--)
                            {
                                if (--each.effects[i].rounds <= 0)
                                {
                                    each.effects.RemoveAt(i);
                                }
                            }
                        }
                    }
                    battle.attackerCast = battle.defenderCast = false;
                    break;
                case EventKind.StackTurn:
                    battle.current = what.a;
                    if (stack != null)
                    {
                        stack.defending = false;
                    }
                    break;
                case EventKind.StackMoved when stack != null:
                    int from = what.b;
                    stack.cell = what.c;
                    stack.facing = (int)(Grid.X2(what.c) >= Grid.X2(from) ? HexSide.East : HexSide.West);
                    break;
                case EventKind.StackAttacked:
                case EventKind.StackShot:
                    BattleStack target = battle.Stack(what.b);
                    if (stack != null && target != null)
                    {
                        stack.facing = (int)(Grid.X2(target.cell) >= Grid.X2(stack.cell) ? HexSide.East : HexSide.West);
                        if (what.kind == EventKind.StackShot && !stack.IsTower)
                        {
                            stack.shots = Mathf.Max(0, stack.shots - 1);
                        }
                    }
                    if (target != null)
                    {
                        Wound(target, what.c, what.d);
                    }
                    break;
                case EventKind.StackDamaged when stack != null:
                    Wound(stack, what.b, what.c);
                    break;
                case EventKind.StackHealed when stack != null:
                    Mend(stack, what.b, what.c);
                    if (what.e == 1)
                    {
                        stack.alive = true;
                        stack.cell = what.d;
                    }
                    break;
                case EventKind.StackDied when stack != null:
                    stack.alive = false;
                    stack.count = 0;
                    break;
                case EventKind.StackDefended when stack != null:
                    stack.defending = true;
                    break;
                case EventKind.StackWaited when stack != null:
                    stack.waited = true;
                    break;
                case EventKind.SpellCast:
                    if (what.a == 0)
                    {
                        battle.attackerCast = true;
                    }
                    else
                    {
                        battle.defenderCast = true;
                    }
                    break;
                case EventKind.EffectAdded when stack != null:
                    var spell = (SpellId)what.b;
                    SpellId opposite = spell == SpellId.Haste ? SpellId.Slow : spell == SpellId.Slow ? SpellId.Haste :
                        spell == SpellId.Bless ? SpellId.Curse : spell == SpellId.Curse ? SpellId.Bless : SpellId.None;
                    stack.effects.RemoveAll(e => e.spell == (int)spell || e.spell == (int)opposite);
                    SpellDef def = Spells.Get(spell);
                    stack.effects.Add(new EffectState { spell = what.b, rounds = what.c, amount = def != null ? def.Amount : 0 });
                    break;
                case EventKind.EffectRemoved when stack != null:
                    stack.effects.RemoveAll(e => e.spell == what.b);
                    break;
            }
        }

        /// <summary>The grid the cells of the battle are counted on: the field's own, or the map's.</summary>
        private HexGrid Grid => Shown.IsField ? Shown.field : World != null ? World.map.grid : Shown.field;

        /// <summary>The state of the game the battle is fought in, for its heroes and players.</summary>
        private GameState World => game != null ? game.State : null;

        /// <summary>The health of one creature of a stack, its hero's bonus included, as the rules count it.</summary>
        private int Each(BattleStack stack)
        {
            HeroState hero = World != null ? World.Hero(Shown.HeroOf(stack.side)) : null;
            return Mathf.Max(1, stack.Def.Health + (game != null ? game.ArmyHealthBonus(hero) : 0));
        }

        /// <summary>
        /// Takes <paramref name="damage"/> from the copy of a stack as the rules took it: the creatures that fall and the
        /// health the first of the rest has left. The count follows the rules' own number of the fallen, should the two
        /// ever differ.
        /// </summary>
        private void Wound(BattleStack stack, int damage, int killed)
        {
            int each = Each(stack);
            int total = stack.count <= 0 ? 0 : (stack.count - 1) * each + Mathf.Clamp(stack.health, 1, each);
            int left = Mathf.Max(0, total - damage);
            int count = Mathf.Max(0, stack.count - killed);
            stack.count = count;
            stack.health = count == 0 ? 0 : Mathf.Clamp(left - (count - 1) * each, 1, each);
        }

        /// <summary>Gives the copy of a stack health back, as the rules gave it: <paramref name="raised"/> creatures stand up again.</summary>
        private void Mend(BattleStack stack, int health, int raised)
        {
            int each = Each(stack);
            int total = stack.count <= 0 ? 0 : (stack.count - 1) * each + Mathf.Clamp(stack.health, 1, each);
            int now = total + Mathf.Max(0, health);
            int count = stack.count + raised;
            stack.count = count;
            stack.health = count == 0 ? 0 : Mathf.Clamp(now - (count - 1) * each, 1, each);
        }

        private IEnumerator Wait(float seconds)
        {
            yield return new WaitForSeconds(seconds / Speed);
        }

        private IEnumerator Turn(int stackId)
        {
            BattleStack stack = Shown.Stack(stackId);
            if (stack == null || field == null)
            {
                yield break;
            }
            field.ClearPaint();
            field.Paint(stack.cell, TurnColor);
            yield return Wait(0.12f);
        }

        private IEnumerator Moved(GameEvent what)
        {
            UnitView view = Stack(what.a);
            BattleStack stack = Shown.Stack(what.a);
            if (view == null)
            {
                yield break;
            }
            field.ClearPaint();
            bool flying = what.d != 0;
            IReadOnlyList<int> path = what.cells;
            float speed = 4.2f * Speed;
            sound?.PlayVaried(Sfx.Footstep, 0.35f);
            if (flying || path == null || path.Count == 0)
            {
                // A flier goes over everything in the way, straight to where it lands.
                Vector3 to = field.Point(what.c);
                yield return view.Turn(to - view.transform.position, 16f);
                view.Puppet?.Walk();
                Vector3 from = view.transform.position;
                float t = 0f;
                float length = Mathf.Max(0.35f, Vector3.Distance(from, to) / (speed * 1.3f));
                while (t < 1f)
                {
                    t += Time.deltaTime / length;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                    Vector3 at = Vector3.Lerp(from, to, k);
                    at.y += Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * (flying ? 2f : 0.25f);
                    view.transform.position = at;
                    yield return null;
                }
                view.transform.position = to;
                view.Puppet?.Idle();
            }
            else
            {
                var points = new List<Vector3>(path.Count);
                foreach (int cell in path)
                {
                    points.Add(field.Point(cell));
                }
                yield return view.WalkPath(points, speed);
            }
            view.Cell = what.c;
            if (stack != null)
            {
                yield return view.Turn(field.Facing((HexSide)stack.facing), 18f);
            }
        }

        private IEnumerator Struck(GameEvent what, bool ranged)
        {
            UnitView attacker = Stack(what.a);
            UnitView target = Stack(what.b);
            if (attacker != null && target != null)
            {
                Vector3 at = target.Middle;
                yield return attacker.Turn(target.transform.position - attacker.transform.position, 18f);
                if (ranged)
                {
                    string clip = string.IsNullOrEmpty(attacker.ShootClip) ? attacker.AttackClip : attacker.ShootClip;
                    if (attacker.Puppet != null && attacker.Puppet.Has(clip))
                    {
                        attacker.Puppet.Play(clip);
                        yield return Wait(0.3f);
                    }
                    sound?.PlayVaried(Sfx.Shoot, 0.8f);
                    ProjectileKind shot = attacker.Projectile != ProjectileKind.None ? attacker.Projectile : ProjectileKind.Arrow;
                    Vector3 from = attacker.Creature == CreatureId.ArrowTower
                        ? attacker.transform.position + Vector3.up * Mathf.Max(2f, attacker.Height * 0.8f)
                        : attacker.Middle;
                    if (effects != null)
                    {
                        yield return effects.Shoot(shot, from, at);
                    }
                }
                else
                {
                    sound?.PlayVaried(Sfx.Swing, 0.8f);
                    yield return attacker.Perform(attacker.AttackClip, target.transform.position);
                }
                sound?.PlayVaried(Sfx.Hit, 0.85f);
                effects?.Spark(Vector3.Lerp(at, attacker.Middle, 0.25f), new Color(1f, 0.85f, 0.55f));
            }
            yield return Damaged(what.b, what.c, what.d, false);
            BattleStack stack = Shown.Stack(what.a);
            if (attacker != null && stack != null && stack.alive)
            {
                StartCoroutine(attacker.Turn(field.Facing((HexSide)stack.facing), 10f));
            }
        }

        private IEnumerator Damaged(int stackId, int damage, int killed, bool quick)
        {
            UnitView view = Stack(stackId);
            BattleStack stack = Shown.Stack(stackId);
            if (view == null)
            {
                yield break;
            }
            effects?.Float(view.Middle + Vector3.up * 0.3f, damage > 0 ? $"-{damage}" : "0", new Color(1f, 0.58f, 0.45f), 3.4f);
            if (killed > 0)
            {
                effects?.Burst(view.Middle, new Color(0.7f, 0.15f, 0.12f), 18, 2.4f);
            }
            if (view.Puppet != null && view.Puppet.Has(view.HitClip))
            {
                view.Puppet.Play(view.HitClip);
            }
            else
            {
                view.Puppet?.Flinch();
            }
            if (badges.TryGetValue(stackId, out Badge badge) && stack != null)
            {
                badge.Set(stack.count, killed > 0);
            }
            yield return Wait(quick ? 0.12f : 0.25f);
        }

        private IEnumerator Healed(GameEvent what)
        {
            BattleStack stack = Shown.Stack(what.a);
            if (stack == null)
            {
                yield break;
            }
            UnitView view = Stack(what.a);
            if (what.e == 1 && view == null && stack.alive)
            {
                // Back from the dead: the stack stands up again where it fell.
                Add(stack);
                view = Stack(what.a);
                effects?.Rise(field.Point(stack.cell) + Vector3.up * 0.3f, new Color(0.75f, 1f, 0.8f), 60);
            }
            if (view == null)
            {
                yield break;
            }
            string text = what.c > 0 ? $"+{what.c}" : $"+{what.b} hp";
            effects?.Float(view.Middle + Vector3.up * 0.3f, text, new Color(0.5f, 1f, 0.6f), what.c > 0 ? 3.4f : 2.6f);
            effects?.Rise(view.Middle, new Color(0.5f, 1f, 0.65f));
            if (badges.TryGetValue(what.a, out Badge badge))
            {
                badge.Set(stack.count, what.c > 0);
            }
            yield return Wait(what.c > 0 ? 0.4f : 0.15f);
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
                badge.Remove();
                badges.Remove(stackId);
            }
            stacks.Remove(stackId);
            BattleStack stack = Shown.Stack(stackId);
            if (stack != null && stack.IsTower)
            {
                sound?.PlayVaried(Sfx.Fireball, 0.7f);
                StartCoroutine(Topple(view, stack.cell));
                yield return Wait(0.6f);
                yield break;
            }
            sound?.PlayVaried(Sfx.Death, 0.8f);
            float length = view.Puppet != null ? view.Puppet.Die(view.DeathClip) : 0.5f;
            StartCoroutine(Bury(view.gameObject, length + 1.2f));
            yield return Wait(0.45f);
        }

        /// <summary>
        /// An arrow tower falls: it shudders, then sinks leaning into a cloud of its own dust with stones flying off it,
        /// and the field leaves the heap of its stones in the wall where it stood (<see cref="IBattlefield.TowerFell"/>).
        /// </summary>
        private IEnumerator Topple(UnitView view, int cell)
        {
            Transform tower = view.transform;
            Vector3 start = tower.position;
            Quaternion upright = tower.rotation;
            float height = Mathf.Max(1.5f, view.Height);
            var dust = new Color(0.7f, 0.64f, 0.56f, 0.9f);
            var stone = new Color(0.55f, 0.56f, 0.6f, 1f);
            // It leans back, away from the besiegers, as it goes down.
            Vector3 axis = Vector3.Cross(Vector3.up, -field.Facing(HexSide.West));
            effects?.Smoke(start + Vector3.up * (height * 0.2f), dust, 50, 2f, 1.5f, -0.04f, 1f);
            float t = 0f;
            while (tower != null && t < 0.4f)
            {
                t += Time.deltaTime;
                tower.position = start + new Vector3(Mathf.Sin(t * 95f), 0f, Mathf.Cos(t * 81f)) * 0.07f;
                yield return null;
            }
            effects?.Burst(start + Vector3.up * (height * 0.75f), stone, 26, 3.4f, 0.22f, 1.3f, 0.5f);
            float puff = 0f;
            t = 0f;
            while (tower != null && t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / 1.2f);
                float fall = t * t;
                tower.position = start + Vector3.down * (fall * height * 1.05f);
                tower.rotation = Quaternion.AngleAxis(fall * 16f, axis) * upright;
                puff -= Time.deltaTime;
                if (puff <= 0f)
                {
                    puff = 0.16f;
                    effects?.Smoke(start + Vector3.up * 0.3f, dust, 22, 1.8f, 1.6f, -0.05f, 1.1f);
                }
                yield return null;
            }
            effects?.Smoke(start + Vector3.up * 0.4f, dust, 60, 1.6f, 2f, -0.03f, 1.4f);
            field?.TowerFell(cell);
            if (tower != null)
            {
                Destroy(tower.gameObject);
            }
        }

        /// <summary>The fallen lie a moment where they fell, then sink into the ground.</summary>
        private IEnumerator Bury(GameObject body, float after)
        {
            yield return new WaitForSeconds(after);
            float t = 0f;
            Vector3 start = body != null ? body.transform.position : Vector3.zero;
            while (body != null && t < 1f)
            {
                t += Time.deltaTime / 1.4f;
                body.transform.position = start + Vector3.down * (t * t * 1.4f);
                yield return null;
            }
            if (body != null)
            {
                Destroy(body);
            }
        }

        private IEnumerator Spell(GameEvent what)
        {
            var spell = (SpellId)what.b;
            SpellDef def = Spells.Get(spell);
            Vector3 at = field.Point(what.c) + Vector3.up * 0.5f;
            BattleStack struck = Shown.StackAt(what.c);
            UnitView target = struck != null ? Stack(struck.id) : null;
            Vector3 middle = target != null ? target.Middle : at;
            HeroView caster = Hero(what.a);
            sound?.Play(Chant(spell, def), 0.9f);
            if (caster != null)
            {
                effects?.Rise(caster.Middle, new Color(0.75f, 0.8f, 1f), 24);
                yield return caster.Cast();
            }
            effects?.Float(middle + Vector3.up * 1.4f, def != null ? def.Name : "Spell", new Color(1f, 0.94f, 0.78f), 3.2f);
            Vector3 source = caster != null ? caster.Middle + Vector3.up * 1f : middle + Vector3.up * 8f;
            switch (spell)
            {
                case SpellId.MagicArrow:
                    if (effects != null)
                    {
                        yield return effects.Shoot(ProjectileKind.None, source, middle);
                    }
                    break;
                case SpellId.IceBolt:
                    if (effects != null)
                    {
                        yield return effects.Shoot(ProjectileKind.Lightning, source, middle);
                    }
                    effects?.Burst(middle, new Color(0.8f, 0.95f, 1f), 40, 3f, 0.2f, 0.4f, 0.3f);
                    break;
                case SpellId.LightningBolt:
                    if (effects != null)
                    {
                        yield return effects.Lightning(middle);
                    }
                    break;
                case SpellId.Fireball:
                    if (effects != null)
                    {
                        yield return effects.Shoot(ProjectileKind.Fire, source, at);
                        yield return effects.Explosion(at, field.CellWidth * 1.3f, new Color(1f, 0.6f, 0.2f));
                    }
                    break;
                case SpellId.MeteorShower:
                    if (effects != null)
                    {
                        yield return effects.Meteors(at, field.CellWidth * 1.5f);
                    }
                    break;
                case SpellId.Implosion:
                    if (effects != null)
                    {
                        yield return effects.Implosion(middle, field.CellWidth * 0.8f);
                    }
                    break;
                default:
                    effects?.Aura(at, def != null && !def.IsPositive ? new Color(0.8f, 0.45f, 0.95f) : new Color(0.65f, 0.8f, 1f));
                    yield return Wait(0.3f);
                    break;
            }
        }

        /// <summary>What a spell sounds like when it is cast.</summary>
        private static Sfx Chant(SpellId spell, SpellDef def)
        {
            switch (spell)
            {
                case SpellId.Fireball:
                case SpellId.MeteorShower:
                    return Sfx.Fireball;
                case SpellId.LightningBolt:
                    return Sfx.Lightning;
                case SpellId.Cure:
                case SpellId.Resurrection:
                case SpellId.AnimateDead:
                    return Sfx.Heal;
                case SpellId.MagicArrow:
                case SpellId.IceBolt:
                case SpellId.Implosion:
                    return Sfx.Spell;
                default:
                    return def != null && !def.IsPositive ? Sfx.Curse : Sfx.Buff;
            }
        }

        private void Aura(int stackId, Color color)
        {
            UnitView view = Stack(stackId);
            if (view != null)
            {
                effects?.Aura(view.transform.position + Vector3.up * 0.05f, color);
            }
        }

        private void Say(int stackId, string text, Color color)
        {
            UnitView view = Stack(stackId);
            if (view != null)
            {
                effects?.Float(view.Middle + Vector3.up * 0.4f, text, color, 2.8f);
            }
        }

        // ------------------------------------------------------------------ the combat log

        /// <summary>The line the combat log gets for an event, told before the copy of the battle takes it.</summary>
        private string Describe(GameEvent what, out int side)
        {
            BattleStack stack = Shown.Stack(what.a);
            side = stack != null ? stack.side : -1;
            switch (what.kind)
            {
                case EventKind.StackAttacked:
                case EventKind.StackShot:
                {
                    BattleStack target = Shown.Stack(what.b);
                    if (stack == null || target == null)
                    {
                        return null;
                    }
                    string verb = what.kind == EventKind.StackShot ? "shoot" : what.e == 1 ? "strike back at" : "attack";
                    if (stack.count == 1 && !stack.IsTower)
                    {
                        verb = what.kind == EventKind.StackShot ? "shoots" : what.e == 1 ? "strikes back at" : "attacks";
                    }
                    else if (stack.IsTower)
                    {
                        verb = "shoots";
                    }
                    return $"{Troop(stack)} {verb} {The(target)}: {what.c} damage{Perish(target, what.d)}.";
                }
                case EventKind.StackDamaged:
                    if (stack == null)
                    {
                        return null;
                    }
                    return $"{Capital(The(stack))} {(stack.count == 1 ? "takes" : "take")} {what.b} damage{Perish(stack, what.c)}.";
                case EventKind.StackHealed:
                    if (stack == null || (what.b <= 0 && what.c <= 0))
                    {
                        return null;
                    }
                    CreatureDef healed = stack.Def;
                    if (what.c > 0)
                    {
                        return $"{what.c} {(what.c == 1 ? healed.Name : healed.Plural)} {(what.e == 1 ? "rise again" : "are restored")}.";
                    }
                    return $"{Capital(The(stack))} {(stack.count == 1 ? "heals" : "heal")} {what.b} health.";
                case EventKind.StackDied:
                    return stack != null ? $"{Capital(The(stack))} {(stack.IsTower ? "falls" : "are wiped out")}." : null;
                case EventKind.StackDefended:
                    return stack != null ? $"{Capital(The(stack))} {(stack.count == 1 ? "stands" : "stand")} on guard." : null;
                case EventKind.StackWaited:
                    return stack != null ? $"{Capital(The(stack))} {(stack.count == 1 ? "waits" : "wait")}." : null;
                case EventKind.MoraleBoost:
                    return stack != null ? $"High morale: {The(stack)} {(stack.count == 1 ? "acts" : "act")} again." : null;
                case EventKind.MoraleFail:
                    return stack != null ? $"Low morale: {The(stack)} {(stack.count == 1 ? "freezes" : "freeze")}." : null;
                case EventKind.LuckyStrike:
                    return stack != null ? $"Good luck: {The(stack)} {(stack.count == 1 ? "strikes" : "strike")} twice as hard." : null;
                case EventKind.SpellCast:
                {
                    side = what.a;
                    HeroState hero = World != null ? World.Hero(what.d) : null;
                    SpellDef def = Spells.Get((SpellId)what.b);
                    return $"{(hero != null ? hero.Name : "The hero")} casts {(def != null ? def.Name : "a spell")}.";
                }
                case EventKind.EffectRemoved:
                {
                    SpellDef def = Spells.Get((SpellId)what.b);
                    return stack != null && def != null ? $"{def.Name} is lifted from {The(stack)}." : null;
                }
                case EventKind.RoundBegan:
                    side = -1;
                    return $"Round {what.a} begins.";
            }
            return null;
        }

        private static string Troop(BattleStack stack)
        {
            CreatureDef def = stack.Def;
            if (stack.IsTower)
            {
                return "The arrow tower";
            }
            return $"{stack.count} {(stack.count == 1 ? def.Name : def.Plural)}";
        }

        private static string The(BattleStack stack)
        {
            CreatureDef def = stack.Def;
            return stack.IsTower ? "the arrow tower" : $"the {(stack.count == 1 ? def.Name : def.Plural)}";
        }

        private static string Perish(BattleStack target, int killed)
        {
            if (killed <= 0)
            {
                return "";
            }
            return killed == 1 ? $", 1 {target.Def.Name} perishes" : $", {killed} {target.Def.Plural} perish";
        }

        private static string Capital(string text)
        {
            return string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
        }

        // ------------------------------------------------------------------ the count by a stack

        /// <summary>
        /// The little plate by a stack that says how many creatures are left in it. It keeps its place in front of the
        /// stack as the camera sees it, and never swings round when the creature turns. It is drawn on a layer over the
        /// field (<see cref="Counts"/>), at the size it would have standing there, so nothing on the field hides it: a
        /// stack behind a crag or the woods of the map, or the wall of a town, still shows where it is and how many.
        /// </summary>
        private sealed class Badge : MonoBehaviour
        {
            /// <summary>The size of the plate's layout units on the field, in meters.</summary>
            private const float Scale = 0.021f;

            private TextMeshProUGUI label;
            private CanvasGroup group;
            private Transform owner;
            private Camera view;
            private float sideways;
            private float size = 1f;
            private int count;
            private float pulse;

            /// <summary>
            /// A plate for <paramref name="owner"/> on <paramref name="layer"/>, <paramref name="size"/> times its size on
            /// the map whatever the size of the creature it counts (a big one is drawn smaller to fit its cell, and its
            /// count must not shrink with it).
            /// </summary>
            public static Badge Make(Transform owner, RectTransform layer, HeroesArt art, Color color, Camera camera, float sideways, float size)
            {
                var holder = new GameObject("Count", typeof(RectTransform), typeof(CanvasGroup));
                var rect = (RectTransform)holder.transform;
                rect.SetParent(layer, false);
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(62f, 30f);

                Color plate = Color.Lerp(color, Color.black, 0.35f);
                plate.a = 0.97f;
                Image fill = Part(rect, "Fill", art != null ? art.bar : null, plate, 1.5f);
                fill.type = Image.Type.Sliced;
                // The borders of the sprites drawn at a size that fits a plate this small (their middles would cross).
                fill.pixelsPerUnitMultiplier = 3f;
                Image rim = Part(rect, "Rim", art != null ? art.slot : null, new Color(1f, 0.9f, 0.62f, 1f), 0f);
                rim.type = Image.Type.Sliced;
                rim.fillCenter = false;
                rim.pixelsPerUnitMultiplier = 3f;

                var text = new GameObject("Text", typeof(TextMeshProUGUI));
                text.transform.SetParent(rect, false);
                var textRect = (RectTransform)text.transform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(2f, 1f);
                textRect.offsetMax = new Vector2(-2f, -1f);
                var label = text.GetComponent<TextMeshProUGUI>();
                label.font = art != null ? art.bodyFont : null;
                label.fontSize = 22f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.color = new Color(1f, 0.96f, 0.86f);
                label.enableAutoSizing = true;
                label.fontSizeMin = 12f;
                label.fontSizeMax = 22f;
                label.textWrappingMode = TextWrappingModes.NoWrap;

                var badge = holder.AddComponent<Badge>();
                badge.label = label;
                badge.group = holder.GetComponent<CanvasGroup>();
                badge.group.blocksRaycasts = false;
                badge.group.interactable = false;
                badge.owner = owner;
                badge.view = camera;
                badge.sideways = sideways;
                badge.size = Mathf.Max(0.1f, size);
                badge.LateUpdate();
                return badge;
            }

            private static Image Part(RectTransform parent, string name, Sprite sprite, Color color, float inset)
            {
                var go = new GameObject(name, typeof(Image));
                go.transform.SetParent(parent, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(inset, inset);
                rect.offsetMax = new Vector2(-inset, -inset);
                Image image = go.GetComponent<Image>();
                image.sprite = sprite;
                image.color = color;
                image.raycastTarget = false;
                return image;
            }

            /// <summary>Shows the count; <paramref name="flash"/> pops the plate to catch the eye when it changed by a blow.</summary>
            public void Set(int value, bool flash = false)
            {
                count = Mathf.Max(0, value);
                label.text = count.ToString();
                if (flash)
                {
                    pulse = 1f;
                }
            }

            public void Remove()
            {
                Destroy(gameObject);
            }

            private void LateUpdate()
            {
                if (view == null)
                {
                    view = Camera.main;
                }
                if (owner == null || view == null || !owner.gameObject.activeInHierarchy)
                {
                    group.alpha = 0f;
                    return;
                }
                // In front of the creature as the camera sees it, a little toward the side it faces from, whichever way
                // the creature itself is looking.
                Vector3 toward = -view.transform.forward;
                toward.y = 0f;
                toward = toward.sqrMagnitude > 1e-4f ? toward.normalized : Vector3.back;
                Vector3 across = view.transform.right;
                across.y = 0f;
                across = across.sqrMagnitude > 1e-4f ? across.normalized : Vector3.right;
                float body = Mathf.Max(0.01f, owner.lossyScale.x);
                Vector3 place = owner.position + (toward * 0.62f + across * (0.5f * sideways) + Vector3.up * 0.16f) * body;
                Vector3 seen = view.WorldToScreenPoint(place);
                if (seen.z <= 0.05f)
                {
                    group.alpha = 0f;
                    return;
                }
                group.alpha = 1f;
                // As big on the screen as it would be standing there: the pixels a meter takes at its distance.
                Vector3 aside = view.WorldToScreenPoint(place + view.transform.right);
                float meter = new Vector2(aside.x - seen.x, aside.y - seen.y).magnitude;
                transform.position = new Vector3(seen.x, seen.y, 0f);
                if (pulse > 0f)
                {
                    pulse = Mathf.Max(0f, pulse - Time.deltaTime * 3f);
                }
                transform.localScale = Vector3.one * (Scale * size * meter * (1f + 0.35f * Mathf.Sin(pulse * Mathf.PI)));
            }
        }
    }
}
