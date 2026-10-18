using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// An artillery shell of the strike mode (<see cref="EnemyShotKind.Shell"/>): fired at a spot, it flies there in
    /// exactly its lifetime (<see cref="StrikeCampaignRules.ShellFlightTime"/>) and bursts, hurting every pilot inside
    /// the burst. It hits nothing on the way. A ring on the playfield marks the spot from the moment it is fired and closes
    /// in as the shell comes down; the shell's body swells and shrinks again, as if it flew an arc toward the camera.
    ///
    /// On the other pilots' clients the shell is a puppet that flies the same course and shows the same ring (the spot
    /// follows from its velocity and age); the burst reaches them as the simulator's blast.
    /// </summary>
    public class EnemyShell : Shot
    {
        [Header("Shell")]
        [Tooltip("The ring that marks where the shell bursts (placed on the playfield every frame).")]
        [SerializeField] internal Transform marker;
        [Tooltip("Size of the marker when the shell is fired and when it lands.")]
        [SerializeField] internal float markerStart = 2.6f;
        [SerializeField] internal float markerEnd = 1.6f;
        [Tooltip("How much bigger the body grows at the top of its arc.")]
        [SerializeField] internal float arc = 0.8f;
        [Tooltip("Radius of the burst (m).")]
        [SerializeField] internal float burst = StrikeCampaignRules.ShellBurstRadius;

        private Vector3 bodyScale = Vector3.one;
        private bool hasScale;

        /// <summary>The spot the shell bursts at: where it will be when its flight time is up.</summary>
        public Vector2 LandingSpot => Position + Velocity * Mathf.Max(0f, FlightTime - Age);

        private float FlightTime => lifetime > 0f ? lifetime : StrikeCampaignRules.ShellFlightTime;

        /// <summary>A shell flies over everything: nothing on the way stops it.</summary>
        internal override bool StopsOnHit => false;


        /// <summary>It never touches a ship on the way: only its burst hurts.</summary>
        internal override bool Touches(Vector2 center, float radius)
        {
            return false;
        }


        public override void OnSpawned()
        {
            if (!hasScale && visual != null)
            {
                bodyScale = visual.localScale;
                hasScale = true;
            }
            base.OnSpawned();
            PlaceMarker();
        }


        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (InPlay)
            {
                PlaceMarker();
                if (visual != null && hasScale)
                {
                    float t = Mathf.Clamp01(Age / FlightTime);
                    visual.localScale = bodyScale * (1f + arc * Mathf.Sin(t * Mathf.PI));
                }
            }
        }


        /// <summary>The ring sits on the landing spot, flat on the playfield, and closes in as the shell comes down.</summary>
        private void PlaceMarker()
        {
            if (marker == null)
            {
                return;
            }
            Vector2 spot = LandingSpot;
            marker.position = new Vector3(spot.x, spot.y, 0.05f);
            marker.rotation = Quaternion.Euler(0f, 0f, Age * 90f);
            float t = Mathf.Clamp01(Age / FlightTime);
            float pulse = 1f + 0.06f * Mathf.Sin(Age * 18f);
            marker.localScale = Vector3.one * (Mathf.Lerp(markerStart, markerEnd, t) * pulse);
        }


        /// <summary>The shell lands: a burst that hurts the pilots in it (the simulator's; it reaches the others as a blast).</summary>
        protected override void Expire()
        {
            if (Field != null && !IsPuppet && !IsGhost)
            {
                Field.Explode(new Blast
                {
                    Center = Position,
                    Radius = burst,
                    Damage = 0f,
                    PlayerDamage = damage,
                    Push = 0f,
                    ByPlayer = false,
                    Tint = impactTint,
                    Layers = Altitude.Air
                });
            }
            Despawn();
        }


        protected override void OnDespawned()
        {
            base.OnDespawned();
            if (visual != null && hasScale)
            {
                visual.localScale = bodyScale;
            }
        }
    }
}
