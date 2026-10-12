using System;
using System.Collections;
using System.Collections.Generic;
using Gamebox;
using TMPro;
using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The 3D backgammon board ("Backgammon" in the scene, on the table at the height of the draughts board's surface):
    /// the box with its felt, 24 points in two colours, the bar and the two trays, fifteen checkers a side, the dice and
    /// the doubling cube. It is laid out from White's side (<see cref="BackgammonLayout"/>); the camera turns, not the
    /// board. What it shows changes through a queue of steps played one after the other: a checker flying from place to
    /// place (a hit checker jumping to the bar as the mover lands, a borne off one standing on its edge in the tray), the
    /// dice thrown onto the roller's half, the cube turned and passed. <see cref="IsAnimating"/> is true while a step
    /// plays, so the manager waits; a move in progress is shown with <see cref="ShowPreview"/> before it is sent.
    /// Marks glow over the points (those whose checkers may move, the targets of the checker picked, the last play), lie
    /// on the checker picked and where it would land, and the dice already played dim; all tinted through property blocks
    /// with the theme's highlight colours.
    /// </summary>
    public sealed class BackgammonBoard : MonoBehaviour
    {
        [Header("Board")]
        [SerializeField] private MeshRenderer frame;
        [SerializeField] private MeshRenderer felt;
        [Tooltip("The 24 points, White's 1-point first.")]
        [SerializeField] private MeshRenderer[] points = new MeshRenderer[24];
        [Tooltip("A glow over each point, shown when it is marked.")]
        [SerializeField] private MeshRenderer[] pointGlows = new MeshRenderer[24];
        [Tooltip("Glows over White's and Black's half of the bar, then over White's and Black's tray.")]
        [SerializeField] private MeshRenderer[] placeGlows = new MeshRenderer[4];

        [Header("Pieces")]
        [Tooltip("The 30 checkers: White's fifteen, then Black's.")]
        [SerializeField] private CheckerView[] checkers = new CheckerView[30];
        [SerializeField] private DiceThrower dice;
        [SerializeField] private Transform cube;
        [SerializeField] private Transform cubeShadow;

        [Header("Marks")]
        [Tooltip("Discs laid on the checker picked and where a move would land.")]
        [SerializeField] private MeshRenderer[] discs = new MeshRenderer[0];
        [Tooltip("The counts written on stacks of more than five checkers.")]
        [SerializeField] private TMP_Text[] labels = new TMP_Text[0];

        [Header("Timing")]
        [SerializeField] private float throwSeconds = 1.05f;
        [SerializeField] private float cubeSeconds = 0.5f;
        [Tooltip("How fast the checkers fly: 1 is the usual pace.")]
        [SerializeField] private float flightSpeed = 1f;

        private const int PerSide = BackgammonGame.CheckersPerSide;
        private const int Places = BackgammonLayout.Places;
        private const int Bar = BackgammonLayout.Bar;
        private const int Off = BackgammonLayout.Off;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly Vector3[] FaceNormals =
        {
            Vector3.up, Vector3.forward, Vector3.right, Vector3.left, Vector3.back, Vector3.down
        };

        /// <summary>Checkers per side and place (side * 26 + place) as the board shows them now.</summary>
        private readonly int[] shown = new int[Places * 2];
        /// <summary>The position of the table, without a move in progress.</summary>
        private readonly int[] committed = new int[Places * 2];
        /// <summary>What the board will show once every queued step has played.</summary>
        private readonly int[] projected = new int[Places * 2];
        private readonly List<CheckerView>[] stacks = new List<CheckerView>[Places * 2];
        private readonly Queue<Step> steps = new Queue<Step>();
        private readonly BackgammonMarks marks = new BackgammonMarks();
        /// <summary>Whose material each die wears (both the roller's, or one each after the opening roll).</summary>
        private readonly Side[] dieSides = { Side.Light, Side.Light };
        /// <summary>The numbers the dice show (0 for a die not shown).</summary>
        private readonly int[] dieValues = new int[2];
        private BoardGamesTheme theme;
        private MaterialPropertyBlock block;
        private MaterialPropertyBlock diceBlock;
        private Coroutine runner;
        private int flying;
        private bool cubeMoving;
        private Side bottom = Side.Light;
        private int knownPhase = -1;
        private Side previewSide = Side.Light;
        private CubeState cubeState;
        private float cubeBob;

        /// <summary>What a queued step does.</summary>
        private enum StepKind
        {
            /// <summary>Checkers fly to the counts of the step.</summary>
            Move,
            /// <summary>Checkers jump to the counts of the step at once.</summary>
            Snap,
            Throw,
            Cube
        }

        private sealed class Step
        {
            public StepKind Kind;
            public int[] Counts;
            /// <summary>The side whose checker moves (the other side's checkers, hit or returning, follow a moment later), or -1.</summary>
            public int Mover = -1;
            public int[] Values;
            public Side Roller;
            public bool Opening;
            public CubeState Cube;
        }

        /// <summary>Where the cube is and what it shows.</summary>
        private struct CubeState
        {
            public bool On;
            public int Value;
            public int Owner;
            public bool Offered;
        }

        /// <summary>Plays a sound of the board (a checker landing, a hit, the dice, the cube); the manager sets it.</summary>
        public Action<ClipKind, float> Sound { get; set; }

        /// <summary>Whether checkers, dice or the cube are moving, or steps wait their turn.</summary>
        public bool IsAnimating => runner != null || steps.Count > 0 || flying > 0 || cubeMoving || (dice != null && dice.IsThrowing);

        /// <summary>The side the camera shows at the bottom (the counts and the cube's numbers read from there).</summary>
        public Side Bottom => bottom;

        /// <summary>Shows or hides the whole board (the box, the checkers, the dice and the cube).</summary>
        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Shows <paramref name="game"/> at once: the checkers where they stand, the dice of the turn on the roller's half,
        /// the cube where it is. Every step still queued is dropped.
        /// </summary>
        public void Show(BackgammonGame game)
        {
            StopAnimations();
            steps.Clear();
            Read(game, committed);
            Array.Copy(committed, projected, projected.Length);
            Sync(committed);
            knownPhase = game != null ? game.phase : -1;
            cubeState = CubeOf(game);
            PlaceCube();
            ShowDice(game);
            ApplyMarks();
        }

        /// <summary>
        /// Shows the moves a person here made so far this turn on top of <paramref name="game"/> (still the position
        /// before them): the checkers fly to where the moves put them, back again after an undo.
        /// </summary>
        public void ShowPreview(BackgammonGame game, IReadOnlyList<CheckerMove> moves)
        {
            if (game == null)
            {
                return;
            }
            Read(game, committed);
            previewSide = game.SideToAct;
            var target = (int[])committed.Clone();
            if (moves != null)
            {
                foreach (CheckerMove move in moves)
                {
                    Apply(target, previewSide, move.from, move.to);
                }
            }
            Enqueue(new Step { Kind = StepKind.Move, Counts = target, Mover = (int)previewSide });
        }

        /// <summary>The moves in progress are given up: the checkers fly back to the table's position.</summary>
        public void CancelPreview()
        {
            if (!Same(projected, committed))
            {
                Enqueue(new Step { Kind = StepKind.Move, Counts = (int[])committed.Clone(), Mover = (int)previewSide });
            }
        }

        /// <summary>The play shown in progress was taken by the table as it was: the board keeps it, nothing flies again.</summary>
        public void CommitPreview(BackgammonGame game)
        {
            Read(game, committed);
            knownPhase = game != null ? game.phase : -1;
            Enqueue(new Step { Kind = StepKind.Snap, Counts = (int[])committed.Clone() });
        }

        /// <summary>
        /// Plays the last play of <paramref name="game"/> checker by checker, from the position before it; the moves the
        /// board already shows (a play in progress here) are not flown again. The board then matches the game exactly.
        /// </summary>
        public void AnimatePlay(BackgammonGame game)
        {
            if (game == null)
            {
                return;
            }
            var after = new int[Places * 2];
            Read(game, after);
            int[] path = game.lastPlay ?? Array.Empty<int>();
            int count = path.Length / 2;
            if (count > 0 && game.lastSide >= 0)
            {
                Side side = game.lastSide == (int)Side.Dark ? Side.Dark : Side.Light;
                var states = new List<int[]> { (int[])committed.Clone() };
                bool replayed = true;
                for (int i = 0; i < count && replayed; i++)
                {
                    var next = (int[])states[i].Clone();
                    replayed = Apply(next, side, path[i * 2], path[i * 2 + 1]);
                    states.Add(next);
                }
                if (replayed)
                {
                    int start = -1;
                    for (int k = count; k >= 0 && start < 0; k--)
                    {
                        if (Same(states[k], projected))
                        {
                            start = k;
                        }
                    }
                    if (start < 0)
                    {
                        Enqueue(new Step { Kind = StepKind.Snap, Counts = states[0] });
                        start = 0;
                    }
                    for (int i = start; i < count; i++)
                    {
                        Enqueue(new Step { Kind = StepKind.Move, Counts = states[i + 1], Mover = (int)side });
                    }
                }
            }
            Array.Copy(after, committed, committed.Length);
            knownPhase = game.phase;
            Enqueue(new Step { Kind = StepKind.Snap, Counts = after });
        }

        /// <summary>
        /// The dice of <paramref name="game"/> were just rolled: they are thrown onto the roller's half, or for the opening
        /// roll one die from each side onto its own half.
        /// </summary>
        public void Roll(BackgammonGame game)
        {
            if (game == null || game.die1 <= 0)
            {
                return;
            }
            bool opening = knownPhase == (int)BackgammonPhase.Opening;
            Side roller = Roller(game);
            knownPhase = game.phase;
            Enqueue(new Step { Kind = StepKind.Throw, Values = new[] { game.die1, game.die2 }, Roller = roller, Opening = opening });
        }

        /// <summary>The cube of <paramref name="game"/>: offered (raised over the bar, turned to the stake offered), taken (in the taker's tray) or in the middle.</summary>
        public void ShowCube(BackgammonGame game, bool animate)
        {
            knownPhase = game != null ? game.phase : knownPhase;
            CubeState state = CubeOf(game);
            if (!animate && steps.Count == 0 && runner == null)
            {
                cubeState = state;
                PlaceCube();
                return;
            }
            Enqueue(new Step { Kind = StepKind.Cube, Cube = state });
        }

        /// <summary>The marks to show (copied); they follow the checkers as the steps play.</summary>
        public void SetMarks(BackgammonMarks shownMarks)
        {
            marks.Clear();
            if (shownMarks != null)
            {
                marks.side = shownMarks.side;
                marks.selected = shownMarks.selected;
                marks.hovered = shownMarks.hovered;
                marks.lastSide = shownMarks.lastSide;
                marks.movable.AddRange(shownMarks.movable);
                marks.targets.AddRange(shownMarks.targets);
                marks.lastFrom.AddRange(shownMarks.lastFrom);
                marks.lastTo.AddRange(shownMarks.lastTo);
                marks.diceLeft = shownMarks.diceLeft != null ? (int[])shownMarks.diceLeft.Clone() : null;
            }
            ApplyMarks();
        }

        /// <summary>The side at the bottom of the screen: the counts and the cube's number turn to face it.</summary>
        public void SetBottom(Side side)
        {
            bottom = side;
            if (!cubeMoving)
            {
                PlaceCube();
            }
            RefreshLabels();
        }

        /// <summary>Whether the checkers stand exactly as in <paramref name="game"/> now (for the tours and checks, once the board is still).</summary>
        public bool ShowsPosition(BackgammonGame game)
        {
            var counts = new int[Places * 2];
            Read(game, counts);
            return Same(counts, shown);
        }

        /// <summary>The checkers <paramref name="side"/> shows at <paramref name="place"/> now (a point, the bar, the tray).</summary>
        public int ShownAt(Side side, int place)
        {
            return place >= 0 && place < Places ? shown[At(side, place)] : 0;
        }

        /// <summary>The place under <paramref name="ray"/>: a point (0 to 23), the bar (24), a tray (25), or -1.</summary>
        public int TargetAt(Ray ray)
        {
            var plane = new Plane(transform.up, transform.position);
            if (!plane.Raycast(ray, out float distance))
            {
                return -1;
            }
            return BackgammonLayout.PlaceAt(transform.InverseTransformPoint(ray.GetPoint(distance)));
        }

        /// <summary>Takes the meshes, materials and colours of <paramref name="look"/>, for what is shown and what comes later.</summary>
        public void ApplyTheme(BoardGamesTheme look)
        {
            theme = look;
            if (look == null)
            {
                return;
            }
            BackgammonLook bg = look.backgammon;
            if (bg != null)
            {
                Dress(frame, bg.frameMesh, bg.frame);
                Dress(felt, bg.feltMesh, bg.felt);
                for (int i = 0; i < points.Length; i++)
                {
                    Dress(points[i], bg.pointMesh, bg.PointOf(i));
                }
                for (int i = 0; i < pointGlows.Length; i++)
                {
                    Dress(pointGlows[i], bg.pointMesh, bg.pointGlow);
                }
                for (int i = 0; i < checkers.Length; i++)
                {
                    if (checkers[i] != null)
                    {
                        checkers[i].Side = i < PerSide ? Side.Light : Side.Dark;
                        checkers[i].Dress(bg.checkerMesh, bg.CheckerOf(checkers[i].Side));
                        checkers[i].SetShadowMaterial(look.contactShadow);
                    }
                }
                for (int i = 0; dice != null && i < dice.Count; i++)
                {
                    Transform die = dice.Die(i);
                    Dress(die != null ? die.GetComponent<MeshRenderer>() : null, bg.dieMesh, null);
                }
                Dress(cube != null ? cube.GetComponent<MeshRenderer>() : null, bg.cubeMesh, bg.cube);
            }
            foreach (MeshRenderer glow in placeGlows)
            {
                Dress(glow, null, look.highlight);
            }
            foreach (MeshRenderer disc in discs)
            {
                Dress(disc, null, look.highlight);
            }
            Dress(cubeShadow != null ? cubeShadow.GetComponent<MeshRenderer>() : null, null, look.contactShadow);
            DressDice(dieSides);
            ApplyMarks();
        }

        /// <summary>Ends every animation at once: the checkers, the dice and the cube jump to where the queued steps take them.</summary>
        public void StopAnimations()
        {
            StopAllCoroutines();
            runner = null;
            flying = 0;
            cubeMoving = false;
            while (steps.Count > 0)
            {
                Step step = steps.Dequeue();
                if (step.Kind == StepKind.Throw)
                {
                    PrepareDice(step);
                    dice?.Show(step.Values);
                }
                else if (step.Kind == StepKind.Cube)
                {
                    cubeState = step.Cube;
                }
            }
            dice?.Stop();
            Sync(projected);
            PlaceCube();
            ApplyMarks();
        }

        private void OnDisable()
        {
            StopAnimations();
        }

        private void LateUpdate()
        {
            if (cube == null || !cube.gameObject.activeSelf)
            {
                return;
            }
            if (cubeState.Offered && !cubeMoving)
            {
                // The offered cube hovers over the bar, gently.
                cubeBob += Time.unscaledDeltaTime;
                cube.localPosition = BackgammonLayout.CubePlace(cubeState.Owner, true) + Vector3.up * (Mathf.Sin(cubeBob * 2.4f) * 0.04f);
            }
            PlaceCubeShadow();
        }

        /// <summary>The cube's shadow under it, on the bar or the felt, smaller while the cube is high.</summary>
        private void PlaceCubeShadow()
        {
            if (cubeShadow == null || cube == null)
            {
                return;
            }
            Vector3 p = cube.localPosition;
            float ground = BackgammonLayout.PlaceAt(p) == Bar ? BackgammonLayout.FrameHeight : 0f;
            float height = Mathf.Max(0f, p.y - ground - BackgammonLayout.CubeSize * 0.5f);
            cubeShadow.localPosition = new Vector3(p.x, ground + 0.004f, p.z);
            float size = BackgammonLayout.CubeSize * 2f * Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(height / 1.2f));
            cubeShadow.localScale = new Vector3(size, 1f, size);
        }

        // ------------------------------------------------------------------ the queue

        private void Enqueue(Step step)
        {
            if (step.Counts != null)
            {
                Array.Copy(step.Counts, projected, projected.Length);
            }
            steps.Enqueue(step);
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                StopAnimations();
                return;
            }
            if (runner == null)
            {
                runner = StartCoroutine(Run());
            }
        }

        private IEnumerator Run()
        {
            while (steps.Count > 0)
            {
                Step step = steps.Dequeue();
                switch (step.Kind)
                {
                    case StepKind.Move:
                        yield return Fly(step.Counts, step.Mover);
                        break;
                    case StepKind.Snap:
                        if (!Same(shown, step.Counts))
                        {
                            Sync(step.Counts);
                        }
                        break;
                    case StepKind.Throw:
                        yield return Throw(step);
                        break;
                    case StepKind.Cube:
                        yield return MoveCube(step.Cube);
                        break;
                }
                ApplyMarks();
            }
            runner = null;
        }

        // ------------------------------------------------------------------ checkers

        private List<CheckerView> Stack(Side side, int place)
        {
            int index = (int)side * Places + place;
            return stacks[index] ??= new List<CheckerView>();
        }

        private static int At(Side side, int place)
        {
            return (int)side * Places + place;
        }

        /// <summary>The checkers of <paramref name="game"/> per side and place.</summary>
        private static void Read(BackgammonGame game, int[] into)
        {
            Array.Clear(into, 0, into.Length);
            if (game == null)
            {
                return;
            }
            for (int point = 0; point < 24 && game.board != null && point < game.board.Length; point++)
            {
                int n = game.board[point];
                if (n > 0)
                {
                    into[At(Side.Light, point)] = n;
                }
                else if (n < 0)
                {
                    into[At(Side.Dark, point)] = -n;
                }
            }
            for (int side = 0; side < 2; side++)
            {
                into[side * Places + Bar] = game.bar != null && game.bar.Length > side ? game.bar[side] : 0;
                into[side * Places + Off] = game.off != null && game.off.Length > side ? game.off[side] : 0;
            }
        }

        /// <summary>One checker of <paramref name="side"/> moves (a lone checker of the other side where it lands goes to the bar); false when there is none to move.</summary>
        private static bool Apply(int[] counts, Side side, int from, int to)
        {
            int own = (int)side * Places;
            int other = (1 - (int)side) * Places;
            if (from < 0 || from > Bar || to < 0 || to >= Places || to == Bar || counts[own + from] <= 0)
            {
                return false;
            }
            counts[own + from]--;
            if (to != Off && counts[other + to] == 1)
            {
                counts[other + to] = 0;
                counts[other + Bar]++;
            }
            counts[own + to]++;
            return true;
        }

        private static bool Same(int[] a, int[] b)
        {
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static bool Shadowed(int place, int index)
        {
            return place != Off && BackgammonLayout.Layer(index) == 0;
        }

        /// <summary>Puts every checker where <paramref name="counts"/> has it, at once.</summary>
        private void Sync(int[] counts)
        {
            for (int s = 0; s < 2; s++)
            {
                var side = (Side)s;
                for (int place = 0; place < Places; place++)
                {
                    Stack(side, place).Clear();
                }
                int next = 0;
                for (int place = 0; place < Places; place++)
                {
                    List<CheckerView> stack = Stack(side, place);
                    for (int k = 0; k < counts[At(side, place)] && next < PerSide; k++)
                    {
                        CheckerView view = s * PerSide + next < checkers.Length ? checkers[s * PerSide + next] : null;
                        next++;
                        if (view == null)
                        {
                            continue;
                        }
                        view.Side = side;
                        stack.Add(view);
                        view.Place(BackgammonLayout.Slot(side, place, stack.Count - 1), Shadowed(place, stack.Count - 1));
                    }
                }
                for (; next < PerSide; next++)
                {
                    CheckerView view = s * PerSide + next < checkers.Length ? checkers[s * PerSide + next] : null;
                    if (view != null)
                    {
                        view.gameObject.SetActive(false);
                    }
                }
            }
            Array.Copy(counts, shown, shown.Length);
            RefreshLabels();
        }

        /// <summary>
        /// The checkers fly from what is shown to <paramref name="target"/>: each place that loses checkers sends its top
        /// ones to a place that gains them, side by side. The other side's checkers (a hit one going to the bar, an
        /// undone hit coming back) leave a moment after the mover.
        /// </summary>
        private IEnumerator Fly(int[] target, int mover)
        {
            for (int s = 0; s < 2; s++)
            {
                var side = (Side)s;
                var from = new List<int>();
                var to = new List<int>();
                for (int place = 0; place < Places; place++)
                {
                    int change = target[At(side, place)] - shown[At(side, place)];
                    for (int i = 0; i < -change; i++)
                    {
                        from.Add(place);
                    }
                    for (int i = 0; i < change; i++)
                    {
                        to.Add(place);
                    }
                }
                float delay = mover >= 0 && s != mover ? 0.3f / Mathf.Max(0.1f, flightSpeed) : 0f;
                for (int i = 0; i < from.Count && i < to.Count; i++)
                {
                    Launch(side, from[i], to[i], delay);
                }
            }
            while (flying > 0)
            {
                yield return null;
            }
            if (!Same(shown, target))
            {
                // Not every change was a flight (a checker missing from the scene): the counts win.
                Array.Copy(target, shown, shown.Length);
                Sync(target);
            }
            RefreshLabels();
        }

        /// <summary>Sends the top checker of <paramref name="side"/> at <paramref name="from"/> to <paramref name="to"/>.</summary>
        private void Launch(Side side, int from, int to, float delay)
        {
            List<CheckerView> source = Stack(side, from);
            if (source.Count == 0)
            {
                return;
            }
            CheckerView view = source[source.Count - 1];
            source.RemoveAt(source.Count - 1);
            shown[At(side, from)]--;
            List<CheckerView> destination = Stack(side, to);
            int index = destination.Count;
            destination.Add(view);
            shown[At(side, to)]++;
            Pose slot = BackgammonLayout.Slot(side, to, index);
            float distance = Vector3.Distance(view.transform.localPosition, slot.position);
            float speed = Mathf.Max(0.1f, flightSpeed);
            float seconds = Mathf.Clamp(0.22f + distance * 0.035f, 0.26f, 0.62f) / speed;
            float hop = to == Bar || from == Bar ? 0.6f : Mathf.Clamp(0.2f + distance * 0.03f, 0.25f, 0.5f);
            flying++;
            StartCoroutine(Flight(view, slot, seconds, hop, delay, Shadowed(to, index), to == Off || to == Bar));
        }

        private IEnumerator Flight(CheckerView view, Pose slot, float seconds, float hop, float delay, bool withShadow, bool thud)
        {
            yield return view.Fly(slot, seconds, hop, delay, withShadow);
            flying = Mathf.Max(0, flying - 1);
            Sound?.Invoke(thud ? ClipKind.BearOff : ClipKind.Clack, 1f);
            RefreshLabels();
        }

        // ------------------------------------------------------------------ dice

        /// <summary>Who threw the dice of <paramref name="game"/>: the side to act while it plays them, else the side that just played or passed.</summary>
        private static Side Roller(BackgammonGame game)
        {
            Side toAct = game.SideToAct;
            return game.Phase == BackgammonPhase.Moving ? toAct : toAct == Side.Light ? Side.Dark : Side.Light;
        }

        /// <summary>The dice of the turn at rest (none before the first roll or during the opening).</summary>
        private void ShowDice(BackgammonGame game)
        {
            if (dice == null)
            {
                return;
            }
            if (game == null || game.die1 <= 0 || game.Phase == BackgammonPhase.Opening)
            {
                dieValues[0] = 0;
                dieValues[1] = 0;
                dice.Hide();
                return;
            }
            var step = new Step { Kind = StepKind.Throw, Values = new[] { game.die1, game.die2 }, Roller = Roller(game) };
            PrepareDice(step);
            dice.Show(step.Values);
        }

        /// <summary>The materials and the rest points of the dice for a throw: both on the roller's half, or one on each half for the opening roll.</summary>
        private void PrepareDice(Step step)
        {
            if (dice == null)
            {
                return;
            }
            for (int i = 0; i < dieValues.Length; i++)
            {
                dieValues[i] = step.Values != null && i < step.Values.Length ? step.Values[i] : 0;
            }
            if (step.Opening)
            {
                dieSides[0] = Side.Light;
                dieSides[1] = Side.Dark;
                dice.SetRest(0, BackgammonLayout.DiceArea(Side.Light) + new Vector3(0f, 0f, 0.05f));
                dice.SetRest(1, BackgammonLayout.DiceArea(Side.Dark) + new Vector3(0f, 0f, -0.05f));
            }
            else
            {
                dieSides[0] = step.Roller;
                dieSides[1] = step.Roller;
                Vector3 area = BackgammonLayout.DiceArea(step.Roller);
                float turn = step.Roller == Side.Light ? 1f : -1f;
                dice.SetRest(0, area + new Vector3(-0.42f * turn, 0f, 0.1f * turn));
                dice.SetRest(1, area + new Vector3(0.42f * turn, 0f, -0.08f * turn));
            }
            DressDice(dieSides);
        }

        private void DressDice(Side[] sides)
        {
            BackgammonLook bg = theme != null ? theme.backgammon : null;
            if (dice == null || bg == null)
            {
                return;
            }
            for (int i = 0; i < dice.Count && i < sides.Length; i++)
            {
                Transform die = dice.Die(i);
                Dress(die != null ? die.GetComponent<MeshRenderer>() : null, null, bg.DieOf(sides[i]));
            }
        }

        /// <summary>Where a side's hand throws from: over its own edge of the box, towards its half.</summary>
        private Vector3 HandOf(Side side)
        {
            Vector3 area = BackgammonLayout.DiceArea(side);
            float z = BackgammonLayout.HalfDepth + 1.2f;
            return transform.TransformPoint(side == Side.Light ? new Vector3(area.x + 0.6f, 0f, -z) : new Vector3(area.x - 0.6f, 0f, z));
        }

        private IEnumerator Throw(Step step)
        {
            if (dice == null)
            {
                yield break;
            }
            PrepareDice(step);
            TintDice();
            Sound?.Invoke(ClipKind.Roll, 1f);
            dice.Bounced -= OnDiceBounced;
            dice.Bounced += OnDiceBounced;
            Vector3[] hands = step.Opening ? new[] { HandOf(Side.Light), HandOf(Side.Dark) } : new[] { HandOf(step.Roller) };
            yield return dice.Throw(step.Values, hands, throwSeconds);
            dice.Bounced -= OnDiceBounced;
        }

        private void OnDiceBounced()
        {
            Sound?.Invoke(ClipKind.Clack, 0.35f);
        }

        /// <summary>
        /// Dims the dice already played (<see cref="BackgammonMarks.diceLeft"/>): by number, or for a double the first die
        /// after two moves and the second after four.
        /// </summary>
        private void TintDice()
        {
            if (dice == null)
            {
                return;
            }
            int[] left = marks.diceLeft;
            var unused = left != null ? new List<int>(left) : null;
            bool isDouble = dieValues[0] > 0 && dieValues[0] == dieValues[1];
            int played = left != null ? 4 - left.Length : 0;
            for (int i = 0; i < dice.Count && i < dieValues.Length; i++)
            {
                Transform die = dice.Die(i);
                var renderer = die != null ? die.GetComponent<MeshRenderer>() : null;
                if (renderer == null)
                {
                    continue;
                }
                bool bright = unused == null || dieValues[i] <= 0 || (isDouble ? played < (i + 1) * 2 : unused.Remove(dieValues[i]));
                if (bright)
                {
                    renderer.SetPropertyBlock(null);
                    continue;
                }
                diceBlock ??= new MaterialPropertyBlock();
                diceBlock.Clear();
                Material material = renderer.sharedMaterial;
                diceBlock.SetColor(BaseColor, new Color(0.45f, 0.45f, 0.45f, 1f));
                diceBlock.SetColor(EmissionColor, material != null && material.HasProperty(EmissionColor) ? material.GetColor(EmissionColor) * 0.2f : Color.black);
                renderer.SetPropertyBlock(diceBlock);
            }
        }

        // ------------------------------------------------------------------ the cube

        private static CubeState CubeOf(BackgammonGame game)
        {
            if (game == null)
            {
                return new CubeState { On = false, Value = 1, Owner = -1 };
            }
            return new CubeState
            {
                On = game.cube,
                Value = Mathf.Max(1, game.cubeValue),
                Owner = game.cubeOwner,
                Offered = game.Phase == BackgammonPhase.Answering
            };
        }

        /// <summary>The face of the cube that shows <paramref name="value"/>: 2 on face 1 up to 64 on face 6; the cube in the middle shows 64.</summary>
        public static int CubeFace(int value)
        {
            int face = 0;
            for (int v = Mathf.Max(1, value); v > 1 && face < 6; v >>= 1)
            {
                face++;
            }
            return face == 0 ? 6 : face;
        }

        /// <summary>The cube's rotation with <paramref name="face"/> on top, its number upright for the player at <paramref name="seenFrom"/>'s side.</summary>
        public static Quaternion CubeRotation(int face, Side seenFrom)
        {
            face = Mathf.Clamp(face, 1, 6);
            Quaternion up = DiceThrower.FaceUp(face, 0f);
            Vector3 normal = FaceNormals[face - 1];
            // The atlas cells stand upright along this direction on each face of the rounded cube.
            Vector3 cellUp = Mathf.Abs(normal.y) > 0.5f ? (normal.y > 0f ? Vector3.forward : Vector3.back) : Vector3.up;
            Vector3 onTop = up * cellUp;
            onTop.y = 0f;
            float yaw = Vector3.SignedAngle(onTop, Vector3.forward, Vector3.up) + (seenFrom == Side.Dark ? 180f : 0f);
            return Quaternion.AngleAxis(yaw, Vector3.up) * up;
        }

        /// <summary>Where the cube of <paramref name="state"/> lies, showing the stake (the one offered while it is offered).</summary>
        private Pose CubePose(CubeState state)
        {
            int shownValue = state.Offered ? state.Value * 2 : state.Value;
            return new Pose(BackgammonLayout.CubePlace(state.Owner, state.Offered), CubeRotation(CubeFace(shownValue), bottom));
        }

        private void PlaceCube()
        {
            if (cube == null)
            {
                return;
            }
            cube.gameObject.SetActive(cubeState.On);
            if (cubeShadow != null)
            {
                cubeShadow.gameObject.SetActive(cubeState.On);
            }
            if (!cubeState.On)
            {
                return;
            }
            Pose pose = CubePose(cubeState);
            cube.SetLocalPositionAndRotation(pose.position, pose.rotation);
            PlaceCubeShadow();
        }

        private IEnumerator MoveCube(CubeState state)
        {
            bool changed = state.On != cubeState.On || state.Value != cubeState.Value || state.Owner != cubeState.Owner || state.Offered != cubeState.Offered;
            cubeState = state;
            if (cube == null || !state.On || !changed || !cube.gameObject.activeSelf)
            {
                PlaceCube();
                yield break;
            }
            cubeMoving = true;
            Sound?.Invoke(ClipKind.Cube, 1f);
            Pose to = CubePose(state);
            Vector3 from = cube.localPosition;
            Quaternion fromRotation = cube.localRotation;
            yield return Tween.Run(cubeSeconds, t =>
            {
                float eased = Tween.InOutCubic(t);
                cube.SetLocalPositionAndRotation(Tween.Arc(from, to.position, 0.45f, eased), Quaternion.Slerp(fromRotation, to.rotation, eased));
            });
            cube.SetLocalPositionAndRotation(to.position, to.rotation);
            cubeBob = 0f;
            cubeMoving = false;
        }

        // ------------------------------------------------------------------ marks

        private static void Dress(MeshRenderer renderer, Mesh mesh, Material material)
        {
            if (renderer == null)
            {
                return;
            }
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
            if (mesh != null)
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter != null)
                {
                    filter.sharedMesh = mesh;
                }
            }
        }

        private void Tint(MeshRenderer renderer, Color color, float strength)
        {
            if (renderer == null)
            {
                return;
            }
            block ??= new MaterialPropertyBlock();
            Color c = color * strength;
            c.a = Mathf.Clamp01(color.a * strength);
            block.SetColor(BaseColor, c);
            renderer.SetPropertyBlock(block);
            if (!renderer.gameObject.activeSelf)
            {
                renderer.gameObject.SetActive(true);
            }
        }

        private MeshRenderer GlowOf(int place, Side side)
        {
            if (place >= 0 && place < 24)
            {
                return place < pointGlows.Length ? pointGlows[place] : null;
            }
            int index = place == Bar ? (int)side : place == Off ? 2 + (int)side : -1;
            return index >= 0 && index < placeGlows.Length ? placeGlows[index] : null;
        }

        /// <summary>The middle of the top of the highest checker of <paramref name="side"/> at <paramref name="place"/>, or null.</summary>
        private Vector3? TopOf(Side side, int place)
        {
            if (place < 0 || place >= Places)
            {
                return null;
            }
            int count = Stack(side, place).Count;
            return count > 0 ? BackgammonLayout.SlotTop(side, place, count - 1) : (Vector3?)null;
        }

        /// <summary>Where a checker of <paramref name="side"/> moved to <paramref name="place"/> would lie (on the felt, or on the tray's floor).</summary>
        private Vector3 LandingOf(Side side, int place, bool hits)
        {
            int index = hits ? 0 : Stack(side, place).Count;
            Pose slot = BackgammonLayout.Slot(side, place, index);
            if (place == Off)
            {
                return new Vector3(slot.position.x, 0.006f, slot.position.z + (side == Side.Light ? 0.5f : -0.5f) * BackgammonLayout.CheckerHeight);
            }
            return slot.position + Vector3.up * 0.006f;
        }

        private void PutDisc(ref int used, Vector3 local, Color color, float strength, float scale)
        {
            if (used >= discs.Length || discs[used] == null)
            {
                used++;
                return;
            }
            MeshRenderer disc = discs[used++];
            disc.transform.localPosition = local + Vector3.up * 0.006f;
            disc.transform.localRotation = Quaternion.identity;
            disc.transform.localScale = new Vector3(scale, 1f, scale);
            Tint(disc, color, strength);
        }

        /// <summary>Redraws the marks over what the board shows now.</summary>
        private void ApplyMarks()
        {
            foreach (MeshRenderer glow in pointGlows)
            {
                Hide(glow);
            }
            foreach (MeshRenderer glow in placeGlows)
            {
                Hide(glow);
            }
            Color selectedColor = theme != null ? theme.selectedColor : new Color(1f, 0.85f, 0.3f, 0.85f);
            Color legalColor = theme != null ? theme.legalColor : new Color(0.35f, 0.9f, 0.45f, 0.8f);
            Color captureColor = theme != null ? theme.captureColor : new Color(1f, 0.45f, 0.3f, 0.9f);
            Color lastColor = theme != null ? theme.lastMoveColor : new Color(0.5f, 0.7f, 1f, 0.35f);
            Color hoverColor = theme != null ? theme.hoverColor : Color.white;
            int used = 0;
            foreach (int place in marks.lastFrom)
            {
                Tint(GlowOf(place, marks.lastSide), lastColor, 0.7f);
            }
            foreach (int place in marks.lastTo)
            {
                Tint(GlowOf(place, marks.lastSide), lastColor, 1f);
            }
            Side side = marks.side;
            Side other = side == Side.Light ? Side.Dark : Side.Light;
            if (marks.selected < 0)
            {
                foreach (int place in marks.movable)
                {
                    Tint(GlowOf(place, side), legalColor, 0.8f);
                }
            }
            else
            {
                Tint(GlowOf(marks.selected, side), selectedColor, 0.75f);
                Vector3? top = TopOf(side, marks.selected);
                if (top.HasValue)
                {
                    PutDisc(ref used, top.Value, selectedColor, 0.6f, 0.86f);
                }
            }
            foreach (int place in marks.targets)
            {
                bool hits = place >= 0 && place < 24 && shown[At(other, place)] == 1;
                Color color = hits ? captureColor : legalColor;
                bool hover = place == marks.hovered;
                if (hover)
                {
                    color = Color.Lerp(color, hoverColor, 0.45f);
                }
                Tint(GlowOf(place, side), color, hover ? 1.15f : 0.9f);
                PutDisc(ref used, LandingOf(side, place, hits), color, 1f, hover ? 0.95f : 0.75f);
            }
            for (; used < discs.Length; used++)
            {
                Hide(discs[used]);
            }
            RefreshLabels();
            TintDice();
        }

        private static void Hide(Component part)
        {
            if (part != null && part.gameObject.activeSelf)
            {
                part.gameObject.SetActive(false);
            }
        }

        /// <summary>Writes the number of checkers on every stack of more than five, on the checker at the tip of the row.</summary>
        private void RefreshLabels()
        {
            int used = 0;
            Quaternion facing = Quaternion.Euler(90f, bottom == Side.Dark ? 180f : 0f, 0f);
            for (int s = 0; s < 2; s++)
            {
                var side = (Side)s;
                for (int place = 0; place <= Bar; place++)
                {
                    int count = Stack(side, place).Count;
                    if (count <= BackgammonLayout.RowLength || used >= labels.Length || labels[used] == null)
                    {
                        continue;
                    }
                    TMP_Text label = labels[used++];
                    int last = BackgammonLayout.RowLength - 1;
                    int layers = (count - 1 - last) / BackgammonLayout.RowLength + 1;
                    Vector3 at = BackgammonLayout.Slot(side, place, last).position + Vector3.up * (layers * BackgammonLayout.CheckerHeight + 0.012f);
                    label.transform.SetLocalPositionAndRotation(at, facing);
                    label.text = count.ToString();
                    label.color = theme != null ? theme.ColorOf(side == Side.Light ? Side.Dark : Side.Light) : (side == Side.Light ? Color.black : Color.white);
                    if (!label.gameObject.activeSelf)
                    {
                        label.gameObject.SetActive(true);
                    }
                }
            }
            for (; used < labels.Length; used++)
            {
                Hide(labels[used]);
            }
        }
    }
}
