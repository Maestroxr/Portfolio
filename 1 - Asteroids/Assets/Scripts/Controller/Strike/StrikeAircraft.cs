using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// An air unit of the strike mode: flies its event's screen-space path (<see cref="FlightPaths"/>) with Catmull-Rom
    /// smoothing at constant speed, faces its velocity and banks into turns, fires its pattern while on screen, can be
    /// rammed, and casts a shadow on the ground. At the end of its path it flies on (Linear) or loops (Repeat, leaving after
    /// <see cref="StrikeRules.RepeatLeaveTime"/>); a kamikaze locks onto the nearest pilot and dives where the rest of
    /// its path would only take it off screen (<see cref="FlightCurve.DiveAt"/>). A puppet flies on
    /// its replicated velocity and faces it.
    ///
    /// The model faces down the screen when the root is not rotated: the root turns so that its -Y points along the
    /// velocity (an aircraft flying straight down is not rotated at all).
    /// </summary>
    public class StrikeAircraft : Enemy
    {
        private enum Flight
        {
            /// <summary>Waiting at the start for its turn in a column (off screen).</summary>
            Waiting,
            /// <summary>Along the path (and round its loop, for a repeating flight).</summary>
            Path,
            /// <summary>Straight on until it leaves (after a linear path, or a loop that ran out of time).</summary>
            Straight,
            /// <summary>A kamikaze's dive at the pilot it locked onto.</summary>
            Dive
        }

        [Header("Strike aircraft")]
        [SerializeField] internal StrikeUnit unit;
        [Tooltip("Flight speed in m/s before the event's speed multiplier.")]
        [SerializeField] internal float baseSpeed = 10f;
        [SerializeField] internal AttackPattern pattern = AttackPattern.Aimed;
        [Tooltip("Volleys per attack.")]
        [SerializeField] internal int burst = 1;
        [Tooltip("Seconds between the volleys of a burst.")]
        [SerializeField] internal float burstGap = 0.15f;
        [Tooltip("Seconds between attacks.")]
        [SerializeField] internal float interval = 1.8f;
        [Tooltip("Where the shots leave; the centre when empty.")]
        [SerializeField] internal Transform[] barrels = new Transform[0];
        [Tooltip("Degrees per second the aircraft turns to face its velocity.")]
        [SerializeField] internal float turnRate = 360f;
        [Tooltip("Degrees of bank per degree per second of turning.")]
        [SerializeField] internal float bankAmount = 0.2f;

        /// <summary>Most bank in degrees either way.</summary>
        private const float MaxBank = 55f;

        /// <summary>Seconds of flying straight on after which an aircraft that is off screen is gone for good.</summary>
        private const float MaxFlightTime = 60f;

        private readonly StrikeGun gun = new StrikeGun();
        private FlightCurve curve;
        private Vector2 offset;
        private Flight flight;
        private FlightType type;
        private float delay;
        private float along;
        private float speed;
        private float flown;
        private Vector2 straight = Vector2.down;
        private float warning;
        private bool warned;
        private float bank;
        private float lastHeading;
        private bool hasRest;
        private Quaternion visualRest = Quaternion.identity;

        public StrikeUnit Unit => unit;

        /// <summary>The event the aircraft was spawned for; null for a puppet.</summary>
        public StrikeEvent Event { get; private set; }

        /// <summary>How the flight ends: the event's type, else the path's default; a kamikaze always dives.</summary>
        public FlightType Type => type;

        /// <summary>The smoothed path it flies (field meters, shared by every flight of it); null for a puppet.</summary>
        public FlightCurve Curve => curve;

        /// <summary>How far its flight is moved from the curve's own place (its start minus the curve's start).</summary>
        public Vector2 CurveOffset => offset;

        /// <summary>Meters flown along the curve.</summary>
        public float Along => along;

        /// <summary>Its flight speed (m/s) along the path.</summary>
        public float Speed => speed;

        /// <summary>Whether it is still waiting for its turn (a later member of a column), off screen.</summary>
        public bool IsWaiting => flight == Flight.Waiting;

        /// <summary>Whether it is a kamikaze diving at the pilot it locked onto.</summary>
        public bool IsDiving => flight == Flight.Dive;

        /// <summary>
        /// An interceptor about to come up from below shows its warning at the bottom edge: the flag travels to the other
        /// pilots' clients, which show the same warning.
        /// </summary>
        internal override bool IsTelegraphing => flight == Flight.Waiting && warned && warning > 0f;

        /// <summary>The gun's clock (for tests and tours).</summary>
        internal StrikeGun Gun => gun;


        /// <summary>
        /// Starts the flight of an event's member at <paramref name="start"/> (screen space, already mirrored and offset by
        /// the formation) after <paramref name="delay"/> seconds, at <paramref name="speedScale"/> times its speed. The
        /// path is the event's, moved so that its first waypoint is <paramref name="start"/>.
        /// </summary>
        public void Launch(StrikeEvent spawn, Vector2 start, float delay, float speedScale)
        {
            Event = spawn;
            FlightPath route = spawn != null ? spawn.path : FlightPath.StraightDown;
            type = unit == StrikeUnit.Kamikaze ? FlightType.Kamikaze
                : spawn != null && spawn.flight != FlightType.Linear ? spawn.flight
                : FlightPaths.DefaultType(route);
            curve = FlightPaths.Curve(route, spawn != null && spawn.mirror, type == FlightType.Repeat);
            offset = start - curve.Start;
            float unitSpeed = baseSpeed > 0f ? baseSpeed : StrikeUnitRules.Info(unit).Speed;
            speed = Mathf.Max(0.5f, unitSpeed * (speedScale > 0f ? speedScale : 1f));
            // An interceptor shows where it will come in before it does.
            warning = unit == StrikeUnit.Interceptor ? StrikeUnitRules.Info(unit).Telegraph : 0f;
            warned = warning <= 0f;
            this.delay = Mathf.Max(0f, delay) + warning;
            flight = Flight.Waiting;
            along = 0f;
            flown = 0f;
            straight = Vector2.down;
            Position = start;
            Velocity = Vector2.zero;
            FaceAt(curve.DirectionAt(0f));
        }


        public override void OnSpawned()
        {
            base.OnSpawned();
            if (!hasRest && visual != null)
            {
                visualRest = visual.localRotation;
                hasRest = true;
            }
            bank = 0f;
            lastHeading = transform.eulerAngles.z;
            StrikeUnitInfo info = StrikeUnitRules.Info(unit);
            gun.Configure(pattern, shotKind, burst, burstGap, interval, info.AlternatePattern, info.AlternateShotKind);
            gun.Restart(interval * Random.Range(0.35f, 0.8f));
            if (IsPuppet)
            {
                Event = null;
                flight = Flight.Straight;
            }
        }


        public override void Tick(float deltaTime)
        {
            if (!IsPuppet && deltaTime > 0f)
            {
                Fly(deltaTime);
            }
            base.Tick(deltaTime);
            if (!InPlay)
            {
                return;
            }
            if (Velocity.sqrMagnitude > 0.01f)
            {
                Turn(Velocity, deltaTime);
            }
            Bank(deltaTime);
            if (!IsPuppet && flight != Flight.Waiting && gun.Tick(deltaTime, CanFireStrike))
            {
                int shots = StrikeUnitRules.Info(unit).Shots;
                if (shots <= 0)
                {
                    shots = barrels != null && barrels.Length > 0 ? barrels.Length : 1;
                }
                FireStrikeVolley(gun.CurrentPattern, gun.CurrentKind, shots, barrels, null, Velocity);
            }
        }


        /// <summary>Sets the velocity that takes it to where its flight is after <paramref name="deltaTime"/>.</summary>
        private void Fly(float deltaTime)
        {
            if (flight != Flight.Waiting)
            {
                flown += deltaTime;
            }
            switch (flight)
            {
                case Flight.Waiting:
                    delay -= deltaTime;
                    if (!warned && delay <= warning)
                    {
                        Warn();
                    }
                    Velocity = Vector2.zero;
                    if (delay <= 0f)
                    {
                        flight = curve != null ? Flight.Path : Flight.Straight;
                        along = -delay * speed;
                        if (flight == Flight.Path)
                        {
                            FlyPath(deltaTime);
                        }
                    }
                    return;
                case Flight.Path:
                    along += speed * deltaTime;
                    FlyPath(deltaTime);
                    return;
                default:
                    Velocity = straight * speed;
                    if (flown > MaxFlightTime && Field != null && Field.Playground != null && !Field.Playground.IsInside(Position))
                    {
                        LeftPlayfield();
                    }
                    return;
            }
        }


        /// <summary>
        /// On along the curve; a repeating flight leaves its loop straight on after <see cref="StrikeRules.RepeatLeaveTime"/>,
        /// and past the end of a curve that does not loop the others fly on. A kamikaze dives where its path would only take
        /// it off screen: at <see cref="FlightCurve.DiveAt"/>, or as soon as the path would carry it off the screen it
        /// came on (every path ends off screen, where the unit would be gone before it could dive).
        /// </summary>
        private void FlyPath(float deltaTime)
        {
            if (type == FlightType.Repeat && curve.Loops && flown >= StrikeRules.RepeatLeaveTime)
            {
                straight = curve.DirectionAt(along);
                flight = Flight.Straight;
                Velocity = straight * speed;
                return;
            }
            if (type == FlightType.Kamikaze && (along >= curve.DiveAt || curve.IsPastEnd(along) || PathLeavesScreen()))
            {
                AsteroidsPlayer ship = Field != null ? Field.NearestShip(Position) : null;
                Vector2 toShip = ship != null ? ship.Position - Position : Vector2.down;
                straight = toShip.sqrMagnitude > 0.01f ? toShip.normalized : Vector2.down;
                speed = StrikeRules.KamikazeDiveSpeed;
                flight = Flight.Dive;
                Velocity = straight * speed;
                Field?.Sounds?.FlyBy();
                return;
            }
            if (curve.IsPastEnd(along))
            {
                straight = curve.EndDirection;
                flight = Flight.Straight;
            }
            MoveTo(offset + curve.PointAt(along), deltaTime);
        }


        /// <summary>Whether it came on screen and this frame's step along the path would take it fully off again.</summary>
        private bool PathLeavesScreen()
        {
            Playground playground = Field != null ? Field.Playground : null;
            if (!HasEntered || playground == null || playground.Wraps)
            {
                return false;
            }
            return !playground.IsInside(offset + curve.PointAt(along), -radius);
        }


        private void MoveTo(Vector2 point, float deltaTime)
        {
            Velocity = deltaTime > 0f ? (point - Position) / deltaTime : Vector2.zero;
        }


        /// <summary>An interceptor warns at the edge where it will come in.</summary>
        private void Warn()
        {
            warned = true;
            if (Field == null || Field.Playground == null)
            {
                return;
            }
            Vector2 edge = Field.Playground.Clamp(Position, 1f);
            Field.Effects?.Telegraph(edge, radius * 1.5f, explosionTint);
            Field.Sounds?.Warning();
        }


        /// <summary>Turns the root so that its -Y (the nose of the model) points along <paramref name="direction"/>.</summary>
        private void Turn(Vector2 direction, float deltaTime)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0f, 0f, angle), turnRate * deltaTime);
        }


        private void FaceAt(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f);
            }
            lastHeading = transform.eulerAngles.z;
        }


        /// <summary>Rolls the model into a turn, by how fast the heading changes.</summary>
        private void Bank(float deltaTime)
        {
            float heading = transform.eulerAngles.z;
            float turning = deltaTime > 0f ? Mathf.DeltaAngle(lastHeading, heading) / deltaTime : 0f;
            lastHeading = heading;
            float wanted = Mathf.Clamp(-turning * bankAmount, -MaxBank, MaxBank);
            bank = Mathf.MoveTowards(bank, wanted, 180f * deltaTime);
            if (visual != null && hasRest)
            {
                // The nose is the root's -Y: the roll is about it.
                visual.localRotation = Quaternion.AngleAxis(bank, Vector3.down) * visualRest;
            }
        }


        /// <summary>First on screen: one more hostile that entered (for the kill star).</summary>
        protected override void OnEnteredScreen()
        {
            Field?.NoteEntered(this);
        }


        /// <summary>The fireball (the enemy's), then on the simulator its drops: a credit orb from half of them and its event's pickup.</summary>
        protected override void OnDestroyed(DamageInfo hit)
        {
            base.OnDestroyed(hit);
            if (IsPuppet || Field == null || Field.Spawner == null)
            {
                return;
            }
            Field.Spawner.DropStrikeSpoils(Event, true, Position);
        }


        protected override void OnDespawned()
        {
            base.OnDespawned();
            Event = null;
            curve = null;
            flight = Flight.Waiting;
            bank = 0f;
            if (visual != null && hasRest)
            {
                visual.localRotation = visualRest;
            }
        }
    }
}
