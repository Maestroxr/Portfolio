using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The flying saucer of the arcade original. It crosses the playfield from one side to the other on a weaving
    /// course and fires as it goes: the big saucer sprays shots anywhere, the small scout aims at the ship. It leaves
    /// the sector on the far side unless it is shot down first.
    /// </summary>
    public class Saucer : Enemy
    {
        [SerializeField] internal float speed = 3.4f;
        [SerializeField] internal float weaveAmplitude = 2.2f;
        [SerializeField] internal float weaveFrequency = 0.7f;
        [Tooltip("Scouts aim at the ship; big saucers fire anywhere.")]
        [SerializeField] internal bool aims;
        [Tooltip("Parts that spin (the rim lights).")]
        [SerializeField] internal Transform rotor;
        [SerializeField] internal AudioSource hum;

        private float baseY;
        private float phase;
        private float direction = 1f;


        /// <summary>Starts a crossing from the left or right edge at height <paramref name="y"/>.</summary>
        public void Enter(bool fromLeft, float y, Playground playground)
        {
            direction = fromLeft ? 1f : -1f;
            float x = playground != null ? (playground.HalfSize.x + radius + 0.5f) * -direction : -20f * direction;
            baseY = y;
            phase = Random.Range(0f, Mathf.PI * 2f);
            Position = new Vector2(x, y);
            Velocity = new Vector2(direction * speed, 0f);
        }


        public override void OnSpawned()
        {
            base.OnSpawned();
            wraps = false;
            if (hum != null)
            {
                hum.Play();
            }
        }


        public override void Tick(float deltaTime)
        {
            if (!IsPuppet)
            {
                float t = Age * weaveFrequency * Mathf.PI * 2f + phase;
                float targetY = baseY + Mathf.Sin(t) * weaveAmplitude;
                Velocity = new Vector2(direction * speed, (targetY - Position.y) * 2.5f);
            }
            base.Tick(deltaTime);
            if (!InPlay)
            {
                return;
            }
            if (rotor != null)
            {
                rotor.localRotation = Quaternion.Euler(0f, 0f, 160f * deltaTime) * rotor.localRotation;
            }
            if (!IsPuppet && ReadyToFire(deltaTime) && Field.Playground != null && Field.Playground.IsInside(Position, 0.5f))
            {
                Fire(aims ? AimAtShip(aimError) : Random.insideUnitCircle.normalized);
            }
        }


        protected override void OnDespawned()
        {
            if (hum != null)
            {
                hum.Stop();
            }
            base.OnDespawned();
        }
    }
}
