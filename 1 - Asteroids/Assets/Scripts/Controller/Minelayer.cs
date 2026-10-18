using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A minelayer: a heavy, slow ship that crosses the sector from one side to the other and sows proximity mines in
    /// its wake, a few at a time. It does not fire; it is tough and worth a lot, and every second it lives the field gets
    /// worse. With a freighter or station to harass it passes close beside it and sows its mines around it, but never
    /// rams it or drops a mine on it.
    /// </summary>
    public class Minelayer : CampaignEnemy
    {
        [SerializeField] internal float speed = 2.2f;
        [SerializeField] internal float layInterval = 2.4f;
        [Tooltip("Mines of this minelayer in play at most.")]
        [SerializeField] internal int maxMines = 5;
        [SerializeField] internal float weave = 1.2f;
        [Tooltip("Meters it keeps between its hull and a freighter's or station's, and lays no mine inside.")]
        [SerializeField] internal float allyClearance = 2.5f;
        [Tooltip("Spins while the minelayer flies (the mine drum).")]
        [SerializeField] internal Transform drum;

        private readonly List<Mine> mines = new List<Mine>();
        private float direction = 1f;
        private float baseY;
        private float phase;
        private float layTimer;


        /// <summary>Starts a crossing from the left or right edge at height <paramref name="y"/>.</summary>
        public void Enter(bool fromLeft, float y, Playground playground)
        {
            direction = fromLeft ? 1f : -1f;
            float x = playground != null ? (playground.HalfSize.x + radius + 0.5f) * -direction : -20f * direction;
            baseY = y;
            phase = Random.Range(0f, Mathf.PI * 2f);
            Position = new Vector2(x, y);
            Velocity = new Vector2(direction * speed, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, fromLeft ? -90f : 90f);
        }


        public override void OnSpawned()
        {
            base.OnSpawned();
            wraps = false;
            mines.Clear();
            layTimer = layInterval * 0.6f;
        }


        public override void Tick(float deltaTime)
        {
            if (!IsPuppet)
            {
                float targetY = baseY + Mathf.Sin(Age * 0.5f + phase) * weave;
                Ally ally = allyRange > 0f ? Ally.Nearest(Field, Position, allyRange) : null;
                if (ally != null)
                {
                    // Pass close beside the freighter or station, on the side the minelayer comes from.
                    float side = Position.y >= ally.Position.y ? 1f : -1f;
                    targetY = Mathf.Lerp(targetY, ally.Position.y + side * (ally.Radius + radius + allyClearance), 0.8f);
                }
                Velocity = new Vector2(direction * speed, Mathf.Clamp((targetY - Position.y) * 1.2f, -speed * 0.6f, speed * 0.6f));
            }
            base.Tick(deltaTime);
            if (!InPlay)
            {
                return;
            }
            Face(Velocity, deltaTime, 90f);
            if (drum != null)
            {
                drum.localRotation = Quaternion.Euler(0f, 0f, 120f * deltaTime) * drum.localRotation;
            }
            if (!IsPuppet && Field != null && Field.Playground != null && Field.Playground.IsInside(Position, 0.5f))
            {
                LayMines(deltaTime);
            }
        }


        private void LayMines(float deltaTime)
        {
            mines.RemoveAll(mine => mine == null || !mine.InPlay);
            layTimer -= deltaTime;
            if (layTimer > 0f || ShotsDisabled || mines.Count >= maxMines || Field.Spawner == null)
            {
                return;
            }
            Ally ally = Ally.Nearest(Field, Position, 12f);
            if (ally != null && Field.Playground.Delta(Position, ally.Position).magnitude < ally.Radius + radius + allyClearance + 0.6f)
            {
                // Too close to a freighter or station: the mine waits until it is clear.
                return;
            }
            layTimer = layInterval * Random.Range(0.85f, 1.15f);
            Vector2 back = Velocity.sqrMagnitude > 0.01f ? -Velocity.normalized : Vector2.down;
            Mine mine = Field.Spawner.SpawnMine(Position + back * (radius + 0.7f), back * 0.6f + Random.insideUnitCircle * 0.3f);
            if (mine != null)
            {
                mines.Add(mine);
                PlayCue(0.5f);
            }
        }


        protected override void OnDespawned()
        {
            base.OnDespawned();
            mines.Clear();
        }
    }
}
