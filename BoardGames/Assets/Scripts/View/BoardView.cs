using System;
using System.Collections;
using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.Checkers
{
    /// <summary>
    /// The 3D board: the slab, a square per cell and a piece per man or king, all from pools so an 8x8 and a 10x10 game
    /// share one scene. Square <c>i</c> of a board of size <c>n</c> sits at local <c>(col - (n-1)/2, surface, row - (n-1)/2)</c>,
    /// row 0 nearest to the Light side. Moves are animated along their path (<see cref="Animate"/>): the manager waits
    /// for <see cref="IsAnimating"/> before it goes on. Highlights tint the squares' quads through a property block.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private float surface = 0.15f;
        [SerializeField] private Transform slab;
        [Tooltip("The table the board stands on: it takes the look's table material.")]
        [SerializeField] private MeshRenderer table;
        [SerializeField] private SquarePool squarePool;
        [SerializeField] private PiecePool piecePool;
        [Tooltip("Seconds a piece takes for one step of its path.")]
        [SerializeField] private float stepSeconds = 0.25f;
        [Tooltip("How much wider than the squares the slab is, in squares.")]
        [SerializeField] private float slabMargin = 0.6f;

        /// <summary>How much of the legal colour's alpha the glow under a piece that may move keeps.</summary>
        private const float MovableAlpha = 0.4f;

        private readonly List<SquareView> squares = new List<SquareView>();
        private readonly Dictionary<int, PieceView> pieces = new Dictionary<int, PieceView>();
        private readonly List<Action> pendingDone = new List<Action>();
        /// <summary>Moves that arrived while one was playing (online, the other side's move can come at once): played in order.</summary>
        private readonly Queue<Pending> queued = new Queue<Pending>();
        private CheckersTheme theme;
        private int size;
        private int running;
        private Color selectedColor = new Color(1f, 0.85f, 0.3f, 0.85f);
        private Color legalColor = new Color(0.35f, 0.9f, 0.45f, 0.8f);
        private Color captureColor = new Color(1f, 0.45f, 0.3f, 0.9f);
        private Color lastMoveColor = new Color(0.5f, 0.7f, 1f, 0.35f);
        private Color hoverColor = new Color(1f, 1f, 1f, 0.9f);

        public float Surface => surface;

        /// <summary>The board shown: 8, 10, or 0 before <see cref="Show"/>.</summary>
        public int Size => size;

        public bool IsAnimating => running > 0;

        /// <summary>
        /// Shows <paramref name="game"/>: squares and slab for its board size, the pieces where they stand. Not
        /// <paramref name="instant"/>, the last move is played from where its piece was (when it is still there).
        /// </summary>
        public void Show(CheckersGame game, bool instant)
        {
            if (game == null || game.board == null)
            {
                Clear();
                return;
            }
            int n = game.board.size;
            if (n != size)
            {
                Layout(n);
            }
            Move last = game.lastMove;
            if (instant || last == null || last.path == null || last.path.Length < 2 || !pieces.ContainsKey(last.From))
            {
                StopAnimations();
                Sync(game.board);
                return;
            }
            Animate(last, game, null);
        }

        /// <summary>
        /// Slides the piece of <paramref name="move"/> along its path, a step at a time; taken pieces lift off and fade
        /// (as the piece passes them in English draughts, at the end of the sequence otherwise), the crown pops on at a
        /// promotion; the board then matches <paramref name="after"/>. <paramref name="done"/> is called exactly once.
        /// </summary>
        public void Animate(Move move, CheckersGame after, Action done)
        {
            if (after == null || after.board == null)
            {
                done?.Invoke();
                return;
            }
            // The position is copied: the game may move on while the animation waits its turn.
            var pending = new Pending
            {
                Move = move,
                Board = after.board.Clone(),
                AsPassed = after.Rules != null && after.Rules.Variant == Variant.English,
                Done = done
            };
            if (running > 0)
            {
                queued.Enqueue(pending);
                return;
            }
            Play(pending);
        }

        /// <summary>A move waiting to be shown, with the position after it.</summary>
        private struct Pending
        {
            public Move Move;
            public Board Board;
            public bool AsPassed;
            public Action Done;
        }

        private void Play(Pending pending)
        {
            if (pending.Board.size != size)
            {
                Layout(pending.Board.size);
            }
            Move move = pending.Move;
            if (move == null || move.path == null || move.path.Length < 2 || !isActiveAndEnabled || !pieces.ContainsKey(move.From))
            {
                Sync(pending.Board);
                pending.Done?.Invoke();
                PlayNext();
                return;
            }
            StartCoroutine(Play(move, pending.Board, pending.AsPassed, pending.Done));
        }

        private void PlayNext()
        {
            if (running == 0 && queued.Count > 0)
            {
                Play(queued.Dequeue());
            }
        }

        /// <summary>
        /// Marks the squares: the selected piece and the squares of a capture in progress, the last move faintly, a dot on
        /// every square a legal move may end on (stronger for a capture, brighter under the pointer), and a faint glow
        /// under every piece of <paramref name="movable"/> (the pieces that may move while nothing is selected).
        /// </summary>
        public void SetHighlights(int selected, IReadOnlyList<Move> moves, Move lastMove, int hovered, int[] partialPath,
            IReadOnlyList<int> movable = null)
        {
            foreach (SquareView square in squares)
            {
                if (square != null)
                {
                    square.Highlight(null, 1f);
                    square.Dot(false);
                }
            }
            if (lastMove != null && lastMove.path != null && lastMove.path.Length >= 2)
            {
                Square(lastMove.From)?.Highlight(lastMoveColor, 1f);
                Square(lastMove.To)?.Highlight(lastMoveColor, 1f);
            }
            if (movable != null)
            {
                Color faint = legalColor;
                faint.a *= MovableAlpha;
                foreach (int square in movable)
                {
                    Square(square)?.Highlight(faint, 1f);
                }
            }
            int steps = partialPath != null ? partialPath.Length : 0;
            for (int i = 1; i < steps; i++)
            {
                Square(partialPath[i])?.Highlight(selectedColor, 0.6f);
            }
            if (selected >= 0)
            {
                Square(selected)?.Highlight(selectedColor, 1f);
            }
            if (moves == null)
            {
                return;
            }
            foreach (Move move in moves)
            {
                if (move == null || move.path == null || move.path.Length < 2)
                {
                    continue;
                }
                int destination = Destination(move, partialPath);
                if (destination < 0)
                {
                    continue;
                }
                SquareView square = Square(destination);
                if (square == null)
                {
                    continue;
                }
                bool hover = destination == hovered;
                Color color = hover ? Color.Lerp(move.IsCapture ? captureColor : legalColor, hoverColor, 0.5f) : move.IsCapture ? captureColor : legalColor;
                square.Dot(color, (move.IsCapture ? 1.3f : 1f) * (hover ? 1.15f : 1f), hover ? 1.3f : 1f);
            }
        }

        /// <summary>The square under <paramref name="ray"/> on the plane of the surface, or -1 off the board.</summary>
        public int SquareAt(Ray ray)
        {
            if (size <= 0)
            {
                return -1;
            }
            var plane = new Plane(transform.up, transform.TransformPoint(new Vector3(0f, surface, 0f)));
            if (!plane.Raycast(ray, out float distance))
            {
                return -1;
            }
            Vector3 local = transform.InverseTransformPoint(ray.GetPoint(distance));
            float half = (size - 1) * 0.5f;
            int col = Mathf.RoundToInt(local.x + half);
            int row = Mathf.RoundToInt(local.z + half);
            if (col < 0 || col >= size || row < 0 || row >= size)
            {
                return -1;
            }
            return Board.Index(size, row, col);
        }

        /// <summary>Where <paramref name="square"/> is, in the world, on the surface.</summary>
        public Vector3 Center(int square)
        {
            return transform.TransformPoint(Local(square));
        }

        /// <summary>Takes the materials and colours of <paramref name="theme"/>, for what is shown and what comes later.</summary>
        public void ApplyTheme(CheckersTheme theme)
        {
            this.theme = theme;
            if (theme == null)
            {
                return;
            }
            selectedColor = theme.selectedColor;
            legalColor = theme.legalColor;
            captureColor = theme.captureColor;
            lastMoveColor = theme.lastMoveColor;
            hoverColor = theme.hoverColor;
            if (slab != null && theme.slab != null)
            {
                var renderer = slab.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = theme.slab;
                }
            }
            if (table != null && theme.table != null)
            {
                table.sharedMaterial = theme.table;
            }
            foreach (SquareView square in squares)
            {
                if (square != null)
                {
                    square.Set(square.Index, square.IsDark, square.IsDark ? theme.darkSquare : theme.lightSquare);
                    square.SetHighlightMaterial(theme.highlight);
                }
            }
            foreach (PieceView piece in pieces.Values)
            {
                Dress(piece, piece.Piece);
            }
        }

        /// <summary>Recycles everything: squares, pieces, animations (their callbacks are called).</summary>
        public void Clear()
        {
            StopAnimations();
            foreach (PieceView piece in pieces.Values)
            {
                piecePool?.Recycle(piece);
            }
            pieces.Clear();
            foreach (SquareView square in squares)
            {
                squarePool?.Recycle(square);
            }
            squares.Clear();
            size = 0;
        }

        private void OnDisable()
        {
            StopAnimations();
        }

        // ------------------------------------------------------------------ layout

        private Vector3 Local(int square)
        {
            float half = (size - 1) * 0.5f;
            return new Vector3(Board.Col(size, square) - half, surface, Board.Row(size, square) - half);
        }

        private SquareView Square(int index)
        {
            return index >= 0 && index < squares.Count ? squares[index] : null;
        }

        private void Layout(int n)
        {
            Clear();
            size = Mathf.Max(n, 2);
            if (slab != null)
            {
                Vector3 scale = slab.localScale;
                slab.localScale = new Vector3(size + slabMargin, scale.y, size + slabMargin);
            }
            if (squarePool == null)
            {
                return;
            }
            for (int i = 0; i < size * size; i++)
            {
                int row = Board.Row(size, i);
                int col = Board.Col(size, i);
                bool dark = Board.Playable(row, col);
                SquareView square = squarePool.Deploy();
                square.transform.SetParent(transform, false);
                square.transform.localPosition = Local(i);
                square.transform.localRotation = Quaternion.identity;
                square.transform.localScale = Vector3.one;
                square.Set(i, dark, theme != null ? (dark ? theme.darkSquare : theme.lightSquare) : null);
                if (theme != null)
                {
                    square.SetHighlightMaterial(theme.highlight);
                }
                squares.Add(square);
            }
        }

        /// <summary>Puts the pieces where <paramref name="board"/> has them, keeping the views that still fit.</summary>
        private void Sync(Board board)
        {
            if (piecePool == null || board == null)
            {
                return;
            }
            var gone = new List<int>();
            foreach (KeyValuePair<int, PieceView> entry in pieces)
            {
                int square = entry.Key;
                sbyte piece = square < board.cells.Length ? board.cells[square] : Pieces.None;
                if (piece == Pieces.None || Pieces.SideOf(piece) != Pieces.SideOf(entry.Value.Piece))
                {
                    gone.Add(square);
                }
            }
            foreach (int square in gone)
            {
                piecePool.Recycle(pieces[square]);
                pieces.Remove(square);
            }
            for (int square = 0; square < board.cells.Length && square < size * size; square++)
            {
                sbyte piece = board.cells[square];
                if (piece == Pieces.None)
                {
                    continue;
                }
                if (!pieces.TryGetValue(square, out PieceView view))
                {
                    view = piecePool.Deploy();
                    view.transform.SetParent(transform, false);
                    view.transform.localRotation = Quaternion.identity;
                    pieces[square] = view;
                }
                view.Square = square;
                Dress(view, piece);
                view.transform.position = Center(square);
            }
        }

        private void Dress(PieceView view, sbyte piece)
        {
            Side side = Pieces.SideOf(piece);
            SideLook look = theme != null && theme.sides != null && theme.sides.Length > (int)side ? theme.sides[(int)side] : null;
            view.Show(piece, look?.piece, view.transform.position);
            view.SetCrownMaterial(look?.crown);
            view.SetShadowMaterial(theme?.contactShadow);
        }

        private static int Destination(Move move, int[] partialPath)
        {
            int steps = partialPath != null ? partialPath.Length : 0;
            if (steps <= 1)
            {
                return move.To;
            }
            if (move.path.Length <= steps)
            {
                return -1;
            }
            for (int i = 0; i < steps; i++)
            {
                if (move.path[i] != partialPath[i])
                {
                    return -1;
                }
            }
            return move.path[steps];
        }

        // ------------------------------------------------------------------ animation

        private void StopAnimations()
        {
            // The queue goes first, so a stopped animation does not start the next one from its finally block.
            var callbacks = new List<Action>();
            while (queued.Count > 0)
            {
                callbacks.Add(queued.Dequeue().Done);
            }
            StopAllCoroutines();
            running = 0;
            callbacks.AddRange(pendingDone);
            pendingDone.Clear();
            foreach (Action done in callbacks)
            {
                done?.Invoke();
            }
        }

        private IEnumerator Play(Move move, Board after, bool asPassed, Action done)
        {
            running++;
            pendingDone.Add(done);
            try
            {
                PieceView piece = pieces[move.From];
                pieces.Remove(move.From);
                int[] path = move.path;
                int[] captured = move.captured ?? Array.Empty<int>();
                bool jump = captured.Length > 0;
                for (int step = 1; step < path.Length; step++)
                {
                    yield return piece.MoveTo(Center(path[step]), stepSeconds, jump ? 0.3f : 0.1f);
                    piece.Square = path[step];
                    if (jump && asPassed && step - 1 < captured.Length)
                    {
                        StartCoroutine(TakeAway(captured[step - 1]));
                    }
                }
                pieces[move.To] = piece;
                if (jump && !asPassed)
                {
                    foreach (int square in captured)
                    {
                        StartCoroutine(TakeAway(square));
                    }
                    yield return Tween.Wait(0.45f);
                }
                if (move.promotes && !piece.IsKing)
                {
                    yield return piece.Crown(0.3f);
                }
                while (running > 1)
                {
                    yield return null;
                }
                Sync(after);
            }
            finally
            {
                running--;
                pendingDone.Remove(done);
                done?.Invoke();
                PlayNext();
            }
        }

        private IEnumerator TakeAway(int square)
        {
            if (!pieces.TryGetValue(square, out PieceView piece))
            {
                yield break;
            }
            pieces.Remove(square);
            running++;
            try
            {
                yield return piece.Lift(0.5f, 0.2f);
                yield return piece.Fade(0.25f);
                piecePool?.Recycle(piece);
            }
            finally
            {
                running--;
            }
        }
    }
}
