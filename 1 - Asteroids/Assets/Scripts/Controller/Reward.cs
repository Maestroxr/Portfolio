using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Abstract base class for pickups: they float where they were dropped, bob and spin, blink before they fade out
    /// and are drawn to the ship when it comes close (or from far away under a tractor magnet). Flying through one
    /// collects it; <see cref="Award"/> is the main method for derived classes to override.
    /// </summary>
    public abstract class Reward : SpaceBody
    {
        [SerializeField] internal string title = "Pickup";
        [SerializeField] internal Color color = Color.white;
        [Tooltip("Parts hidden and shown while the pickup blinks before fading.")]
        [SerializeField] internal Renderer[] blinkRenderers = new Renderer[0];
        [Tooltip("Distance from which the pickup drifts to the ship on its own.")]
        [SerializeField] internal float attractRange = 2.4f;
        [SerializeField] internal float bobHeight = 0.12f;

        private bool visible = true;

        /// <summary>The local ship touched the pickup in a shared mission and asked the server for it.</summary>
        internal bool Claimed { get; set; }

        /// <summary>Kept from the original: the shot that caused the drop, if any.</summary>
        public Shot Cause;

        public virtual string Title => title;

        public Color Color => color;

        /// <summary>Pickups cannot be grabbed in the first instant, so they are seen popping out first.</summary>
        public bool CanBeCollected => Age > 0.15f;


        public override void OnSpawned()
        {
            base.OnSpawned();
            Claimed = false;
            SetVisible(true);
            if (visual != null)
            {
                visual.localRotation = Quaternion.identity;
            }
        }


        public override void Tick(float deltaTime)
        {
            Attract(deltaTime);
            base.Tick(deltaTime);
            if (!InPlay)
            {
                return;
            }
            if (visual != null)
            {
                visual.localPosition = new Vector3(0f, 0f, -Mathf.Abs(Mathf.Sin(Age * 3f)) * bobHeight);
            }
            if (lifetime > 0f && lifetime - Age < 3f)
            {
                float rate = lifetime - Age < 1.2f ? 12f : 6f;
                SetVisible(Mathf.Sin(Age * rate * Mathf.PI) > -0.3f);
            }
        }


        private void Attract(float deltaTime)
        {
            // A puppet only drifts to a halt by itself; where the real pickup is drawn to comes from the simulator.
            AsteroidsPlayer ship = Field != null && !IsPuppet ? Field.NearestShip(Position) : null;
            if (ship == null || Field.Playground == null)
            {
                Velocity *= Mathf.Clamp01(1f - deltaTime * 0.8f);
                return;
            }
            float range = ship.IsPowerUpActive(PowerUpType.Magnet) ? 14f : attractRange;
            Vector2 toShip = Field.Playground.Delta(Position, ship.Position);
            float distance = toShip.magnitude;
            if (distance > range || distance < 0.001f)
            {
                Velocity *= Mathf.Clamp01(1f - deltaTime * 0.8f);
                return;
            }
            float pull = Mathf.Lerp(26f, 8f, distance / range);
            Velocity = Vector2.MoveTowards(Velocity, toShip / distance * Mathf.Max(6f, distance * 3f), pull * deltaTime);
        }


        /// <summary>The ship flew through the pickup.</summary>
        public void Collect(AsteroidsPlayer player)
        {
            Award(player);
            if (Field != null)
            {
                Field.Effects?.Pickup(Position, color);
                Field.Sounds?.Pickup(this);
            }
            Exit = ExitReason.Collected;
            Despawn();
        }


        /// <summary>A pilot on another device collected the pickup (<paramref name="seat"/>): it goes without giving anything here.</summary>
        internal void CollectedElsewhere(int seat)
        {
            if (!InPlay)
            {
                return;
            }
            Field?.Effects?.Pickup(Position, color);
            Exit = ExitReason.Collected;
            ExitSeat = seat;
            Despawn();
        }


        /// <summary>Gives the pickup's effect to <paramref name="player"/>.</summary>
        public abstract void Award(AsteroidsPlayer player);


        private void SetVisible(bool show)
        {
            if (visible == show || blinkRenderers == null)
            {
                return;
            }
            visible = show;
            foreach (Renderer part in blinkRenderers)
            {
                if (part != null)
                {
                    part.enabled = show;
                }
            }
        }


        protected override void OnDespawned()
        {
            base.OnDespawned();
            SetVisible(true);
            Cause = null;
        }
    }
}
