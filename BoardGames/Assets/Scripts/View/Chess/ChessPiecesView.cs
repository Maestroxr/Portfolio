using System;
using System.Collections;
using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The chess pieces on the 3D board ("Board/ChessPieces"): a <see cref="ChessPieceView"/> from the pool on every
    /// occupied square, at the board's square centres (<see cref="BoardView.Center"/>; square <c>rank * 8 + file</c>,
    /// rank 0 on Light's side). <see cref="Animate"/> plays a move: the piece slides (a knight hops), a taken piece lifts
    /// off and fades as the mover arrives (en passant: the pawn beside), the rook of a castling follows the king, and a
    /// promoted pawn pops up as its new piece. Moves that arrive while one plays wait their turn; the manager waits for
    /// <see cref="IsAnimating"/> before it goes on.
    /// </summary>
    public sealed class ChessPiecesView : MonoBehaviour
    {
        [SerializeField] private BoardView board;
        [SerializeField] private ChessPiecePool pool;
        [Tooltip("Seconds of a slide one square long; longer slides take a little longer.")]
        [SerializeField] private float slideSeconds = 0.24f;
        [Tooltip("How high a knight jumps.")]
        [SerializeField] private float knightHop = 0.6f;
        [Tooltip("How high the rook of a castling jumps over the king.")]
        [SerializeField] private float rookHop = 0.45f;

        private const int Squares = 64;

        private readonly ChessPieceView[] pieces = new ChessPieceView[Squares];
        private readonly List<Action> pendingDone = new List<Action>();
        /// <summary>Moves that arrived while one was playing (online, the other side's move can come at once): played in order.</summary>
        private readonly Queue<Pending> queued = new Queue<Pending>();
        private ChessLook look;
        private Material shadowMaterial;
        private Material glowMaterial;
        private int running;

        /// <summary>True while a move plays or waits to.</summary>
        public bool IsAnimating => running > 0 || queued.Count > 0;

        /// <summary>The piece on <paramref name="square"/>, or null.</summary>
        public ChessPieceView At(int square)
        {
            return square >= 0 && square < Squares ? pieces[square] : null;
        }

        /// <summary>Takes the chess look, the contact shadow and the highlight material of <paramref name="theme"/>, for what is shown and what comes later.</summary>
        public void ApplyLook(BoardGamesTheme theme)
        {
            if (theme == null)
            {
                return;
            }
            look = theme.chess;
            shadowMaterial = theme.contactShadow;
            glowMaterial = theme.highlight;
            // Every live view, a piece in flight (in no square while it moves) among them.
            IEnumerable<ChessPieceView> live = pool != null ? pool.Deployed : (IEnumerable<ChessPieceView>)pieces;
            foreach (ChessPieceView piece in live)
            {
                if (piece != null)
                {
                    piece.SetLook(look, shadowMaterial, glowMaterial);
                }
            }
        }

        /// <summary>
        /// The pieces a move of the piece picked up may take glow around their foot in <paramref name="color"/> (brighter
        /// the one under the pointer, <paramref name="hovered"/>): the board's dot would hide under them. The others stop
        /// glowing; null or empty clears every glow.
        /// </summary>
        public void MarkTakeable(IReadOnlyList<int> squares, Color color, int hovered)
        {
            for (int square = 0; square < Squares; square++)
            {
                ChessPieceView piece = pieces[square];
                if (piece == null)
                {
                    continue;
                }
                bool marked = false;
                if (squares != null)
                {
                    for (int i = 0; i < squares.Count; i++)
                    {
                        if (squares[i] == square)
                        {
                            marked = true;
                            break;
                        }
                    }
                }
                piece.Glow(marked ? color : (Color?)null, square == hovered ? 1.35f : 1f);
            }
        }

        /// <summary>Shows <paramref name="game"/>'s position at once (any animation stops); null clears the board.</summary>
        public void Show(ChessGame game)
        {
            Show(game != null ? game.cells : null);
        }

        /// <summary>Shows the cells of a position at once (any animation stops); null clears the board.</summary>
        public void Show(sbyte[] cells)
        {
            if (IsAnimating)
            {
                StopAnimations();
            }
            if (cells == null)
            {
                Clear();
                return;
            }
            Sync(cells);
        }

        /// <summary>
        /// Plays <paramref name="move"/>, after which the board shows <paramref name="after"/> (copied: the game may move
        /// on while the animation waits its turn). <paramref name="done"/> is called exactly once, also when the
        /// animation is stopped.
        /// </summary>
        public void Animate(ChessMove move, ChessGame after, Action done)
        {
            if (after == null || after.cells == null)
            {
                done?.Invoke();
                return;
            }
            var pending = new Pending
            {
                Move = move != null ? move.Clone() : null,
                Cells = (sbyte[])after.cells.Clone(),
                Done = done
            };
            if (running > 0)
            {
                queued.Enqueue(pending);
                return;
            }
            Play(pending);
        }

        /// <summary>Recycles every piece and stops the animations (their callbacks are called).</summary>
        public void Clear()
        {
            StopAnimations();
            for (int square = 0; square < Squares; square++)
            {
                if (pieces[square] != null)
                {
                    pool?.Recycle(pieces[square]);
                    pieces[square] = null;
                }
            }
        }

        /// <summary>The square of the nearest piece <paramref name="ray"/> meets, or -1 (the board's plane is the caller's).</summary>
        public int SquareAt(Ray ray)
        {
            int found = -1;
            float best = float.MaxValue;
            for (int square = 0; square < Squares; square++)
            {
                ChessPieceView piece = pieces[square];
                if (piece != null && piece.isActiveAndEnabled && piece.Hit(ray, out float distance) && distance < best)
                {
                    best = distance;
                    found = square;
                }
            }
            return found;
        }

        /// <summary>Where <paramref name="square"/> is, in the world, on the board's surface.</summary>
        public Vector3 Center(int square)
        {
            if (board != null && board.Size == 8)
            {
                return board.Center(square);
            }
            // No board laid out (a preview): the same layout from this object.
            float surface = board != null ? board.Surface : 0.15f;
            return transform.TransformPoint(new Vector3(square % 8 - 3.5f, surface, square / 8 - 3.5f));
        }

        private void OnDisable()
        {
            StopAnimations();
        }

        // ------------------------------------------------------------------ the pieces

        private Quaternion BoardRotation => board != null ? board.transform.rotation : transform.rotation;

        /// <summary>Puts the pieces where <paramref name="cells"/> has them, keeping the views that still fit.</summary>
        private void Sync(sbyte[] cells)
        {
            // The views of squares that are empty now go back first: a jump to a far position (a loaded game) would
            // otherwise deploy more views than the pool holds before the old ones return.
            for (int square = 0; square < Squares; square++)
            {
                sbyte piece = square < cells.Length ? cells[square] : ChessPieces.None;
                if (piece == ChessPieces.None && pieces[square] != null)
                {
                    pool?.Recycle(pieces[square]);
                    pieces[square] = null;
                }
            }
            for (int square = 0; square < Squares; square++)
            {
                sbyte piece = square < cells.Length ? cells[square] : ChessPieces.None;
                ChessPieceView view = pieces[square];
                if (piece == ChessPieces.None)
                {
                    continue;
                }
                if (view == null)
                {
                    if (pool == null)
                    {
                        continue;
                    }
                    view = pool.Deploy();
                    view.transform.SetParent(transform, false);
                    view.SetLook(look, shadowMaterial, glowMaterial);
                    pieces[square] = view;
                }
                view.Square = square;
                view.Show(piece, Center(square), BoardRotation);
            }
        }

        // ------------------------------------------------------------------ animation

        /// <summary>A move waiting to be shown, with the cells after it.</summary>
        private struct Pending
        {
            public ChessMove Move;
            public sbyte[] Cells;
            public Action Done;
        }

        private void Play(Pending pending)
        {
            ChessMove move = pending.Move;
            if (move == null || !isActiveAndEnabled || move.from < 0 || move.from >= Squares || move.to < 0 || move.to >= Squares || pieces[move.from] == null)
            {
                Sync(pending.Cells);
                pending.Done?.Invoke();
                PlayNext();
                return;
            }
            StartCoroutine(Play(move, pending.Cells, pending.Done));
        }

        private void PlayNext()
        {
            if (running == 0 && queued.Count > 0)
            {
                Play(queued.Dequeue());
            }
        }

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
            // A piece in flight or being taken is in no square: every view is let go, the next Show deploys afresh.
            if (pool != null)
            {
                foreach (ChessPieceView view in pool.Deployed)
                {
                    pool.Recycle(view);
                }
            }
            Array.Clear(pieces, 0, Squares);
            foreach (Action done in callbacks)
            {
                done?.Invoke();
            }
        }

        private IEnumerator Play(ChessMove move, sbyte[] after, Action done)
        {
            running++;
            pendingDone.Add(done);
            try
            {
                ChessPieceView mover = pieces[move.from];
                pieces[move.from] = null;
                ChessPieceView taken = null;
                if (move.IsCapture && move.captured >= 0 && move.captured < Squares && move.captured != move.from)
                {
                    taken = pieces[move.captured];
                    pieces[move.captured] = null;
                }
                int files = Mathf.Abs(move.to % 8 - move.from % 8);
                int ranks = Mathf.Abs(move.to / 8 - move.from / 8);
                bool knight = mover.Kind == ChessPieceKind.Knight;
                float seconds = slideSeconds * (0.75f + 0.25f * Mathf.Sqrt(files * files + ranks * ranks));
                if (knight)
                {
                    seconds = slideSeconds * 1.6f;
                }
                if (taken != null)
                {
                    // The taken piece leaves as the mover comes in (en passant: as it passes beside it).
                    StartCoroutine(TakeAway(taken, seconds * (move.IsEnPassant ? 0.8f : 0.6f)));
                }
                yield return mover.MoveTo(Center(move.to), seconds, knight ? knightHop : 0.05f);
                mover.Square = move.to;
                pieces[move.to] = mover;
                if (move.IsCastle && move.RookFrom >= 0 && pieces[move.RookFrom] != null)
                {
                    ChessPieceView rook = pieces[move.RookFrom];
                    pieces[move.RookFrom] = null;
                    yield return rook.MoveTo(Center(move.RookTo), slideSeconds * 1.5f, rookHop);
                    rook.Square = move.RookTo;
                    pieces[move.RookTo] = rook;
                }
                if (move.IsPromotion)
                {
                    yield return mover.Promote(ChessPieces.Make(mover.Side, (ChessPieceKind)move.promotion), 0.4f);
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

        private IEnumerator TakeAway(ChessPieceView piece, float delay)
        {
            running++;
            try
            {
                yield return Tween.Wait(delay);
                yield return piece.Lift(0.45f, 0.2f);
                yield return piece.Fade(0.25f);
                pool?.Recycle(piece);
            }
            finally
            {
                running--;
            }
        }
    }
}
