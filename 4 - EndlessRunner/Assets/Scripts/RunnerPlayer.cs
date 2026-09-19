using Gamebox;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// The runner. Runs down three lanes on a <see cref="CharacterController"/>: switches lanes, jumps, slides under
    /// barriers and walks up ramps onto wagons. Crashes, side bumps and falls are reported to the
    /// <see cref="RunnerGameManager"/>, which decides what they cost.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(CharacterController))]
    public class RunnerPlayer : PlayerBase
    {
        /// <summary>The runner lives on the Ignore Raycast layer so its own queries can skip it.</summary>
        public const int Layer = 2;
        private const int QueryMask = ~(1 << Layer);

        [Header("Movement")]
        [SerializeField] internal float laneWidth = LayoutBuilder.LaneWidth;
        [SerializeField] internal float gravity = 32f;
        [SerializeField] internal float fastFallSpeed = 26f;
        [SerializeField] internal float slideDuration = 0.75f;
        [SerializeField] internal float standHeight = 1.7f;
        [SerializeField] internal float slideHeight = 0.75f;
        [SerializeField] internal float coyoteTime = 0.12f;
        [SerializeField] internal float jumpBuffer = 0.18f;
        [SerializeField] internal float superJumpMultiplier = 2.3f;
        [SerializeField] internal float recoverAcceleration = 9f;
        [SerializeField] internal float fallLimit = -3f;

        [Header("Presentation")]
        [SerializeField] internal RunnerAnimator animator;
        [SerializeField] internal GameObject shieldBubble;
        [SerializeField] internal GameObject magnetAura;
        [SerializeField] internal GameObject superJumpAura;
        [SerializeField] internal ParticleSystem runDust;

        private readonly RunnerInput input = new RunnerInput();
        private CharacterController body;
        private float targetSpeed;
        private int previousLane;
        private float bumpCooldown;
        private float lastGroundedTime = -10f;
        private float jumpRequestTime = -10f;
        private float slideTimer;
        private bool slideQueued;
        private float invulnerableUntil;
        private int stallFrames;

        public float Speed { get; private set; }
        public int Lane { get; private set; }
        public bool IsGrounded { get; private set; } = true;
        public bool IsSliding { get; private set; }
        public bool IsRunning { get; private set; }
        public float VerticalSpeed { get; private set; }
        public bool SuperJump { get; set; }
        public float JumpHeight { get; private set; } = 1.8f;
        public float SideSpeed { get; private set; } = 14f;
        public float Gravity => gravity;
        public float LaneWidth => laneWidth;
        public bool IsInvulnerable => Time.time < invulnerableUntil;
        public float InvulnerableTimeLeft => Mathf.Max(0f, invulnerableUntil - Time.time);

        /// <summary>Position at the start of this frame's move, for swept pickup tests.</summary>
        public Vector3 PreviousPosition { get; private set; }

        public float Height => body != null ? body.height : standHeight;

        /// <summary>-1 or 1 while switching lanes, 0 otherwise.</summary>
        public int LaneChangeDirection { get; private set; }

        private RunnerGameManager Runner => GameManager as RunnerGameManager;

        private void Awake()
        {
            body = GetComponent<CharacterController>();
            PreviousPosition = transform.position;
        }

        public void ApplySettings(RunnerSettings settings)
        {
            if (settings == null)
            {
                return;
            }
            JumpHeight = settings.JumpHeight;
            SideSpeed = settings.SideSpeed;
        }

        /// <summary>Puts the runner on the start line, standing still.</summary>
        public void ResetToStart(Vector3 position)
        {
            IsRunning = false;
            Speed = 0f;
            targetSpeed = 0f;
            Lane = 0;
            previousLane = 0;
            VerticalSpeed = 0f;
            IsSliding = false;
            slideQueued = false;
            SetHeight(standHeight);
            invulnerableUntil = 0f;
            SuperJump = false;
            jumpRequestTime = -10f;
            stallFrames = 0;
            Teleport(position);
            IsGrounded = true;
            SetAuras(false, false, false);
            SetDust(0f);
            if (animator != null)
            {
                animator.ResetPose();
            }
        }

        public void BeginRun()
        {
            IsRunning = true;
            lastGroundedTime = Time.time;
        }

        public void StopRun()
        {
            IsRunning = false;
            IsSliding = false;
            SetHeight(standHeight);
            SetDust(0f);
        }

        public void SetTargetSpeed(float speed)
        {
            targetSpeed = speed;
        }

        /// <summary>Scales the current speed, e.g. after a crash. The runner accelerates back to the target speed.</summary>
        public void Slow(float factor)
        {
            Speed *= factor;
        }

        public void MakeInvulnerable(float seconds)
        {
            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
        }

        /// <summary>Throws the runner into the air, as a bounce pad does.</summary>
        public void Launch(float height)
        {
            EndSlide();
            VerticalSpeed = Mathf.Sqrt(2f * gravity * height);
            IsGrounded = false;
            lastGroundedTime = -10f;
            jumpRequestTime = -10f;
            slideQueued = false;
            if (animator != null)
            {
                animator.Flip();
            }
        }

        /// <summary>Moves the runner without sweeping (the controller would otherwise collide on the way).</summary>
        public void Teleport(Vector3 position)
        {
            if (body == null)
            {
                body = GetComponent<CharacterController>();
            }
            bool enabledBefore = body.enabled;
            body.enabled = false;
            transform.position = position;
            body.enabled = enabledBefore;
            PreviousPosition = position;
        }

        /// <summary>Puts the runner back on solid ground after a fall.</summary>
        public void Respawn(Vector3 position)
        {
            Teleport(position);
            VerticalSpeed = 0f;
            IsGrounded = true;
            EndSlide();
            previousLane = Lane;
        }

        /// <summary>Undoes the lane switch that ran into the side of an obstacle.</summary>
        public void BumpBack()
        {
            Lane = previousLane;
            bumpCooldown = 0.3f;
            if (animator != null)
            {
                animator.Stumble();
            }
        }

        /// <summary>Presses a control from code: on-screen buttons or the autopilot.</summary>
        public void Press(RunnerAction action)
        {
            input.Press(action);
        }

        /// <summary><see cref="Press(RunnerAction)"/> by name ("Left", "Right", "Jump", "Slide"), for SendMessage.</summary>
        public void PressNamed(string action)
        {
            if (System.Enum.TryParse(action, true, out RunnerAction parsed))
            {
                Press(parsed);
            }
        }

        public void SetAuras(bool shield, bool magnet, bool superJump)
        {
            SetActive(shieldBubble, shield);
            SetActive(magnetAura, magnet);
            SetActive(superJumpAura, superJump);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private void Update()
        {
            PreviousPosition = transform.position;
            if (!IsRunning || GameManager == null || !GameManager.IsGameRunning)
            {
                return;
            }
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            input.Read();
            if (input.Left)
            {
                ChangeLane(-1);
            }
            if (input.Right)
            {
                ChangeLane(1);
            }
            if (input.Jump)
            {
                jumpRequestTime = Time.time;
            }
            if (input.Slide)
            {
                RequestSlide();
            }
            bumpCooldown -= deltaTime;

            float rate = Speed < targetSpeed - 0.5f ? recoverAcceleration : 30f;
            Speed = Mathf.MoveTowards(Speed, targetSpeed, rate * deltaTime);

            float x = transform.position.x;
            float targetX = Lane * laneWidth;
            float newX = Mathf.MoveTowards(x, targetX, SideSpeed * deltaTime);
            LaneChangeDirection = Mathf.Abs(targetX - x) > 0.05f ? (int)Mathf.Sign(targetX - x) : 0;

            bool canJump = IsGrounded || Time.time - lastGroundedTime <= coyoteTime;
            if (Time.time - jumpRequestTime <= jumpBuffer && canJump && VerticalSpeed <= 0.1f)
            {
                DoJump();
            }
            if (IsSliding)
            {
                slideTimer -= deltaTime;
                if (slideTimer <= 0f && HasHeadroom())
                {
                    EndSlide();
                }
            }

            VerticalSpeed -= gravity * deltaTime;
            float startZ = transform.position.z;
            CollisionFlags flags = body.Move(new Vector3(newX - x, VerticalSpeed * deltaTime, Speed * deltaTime));
            bool wasGrounded = IsGrounded;
            IsGrounded = body.isGrounded;
            if (IsGrounded)
            {
                lastGroundedTime = Time.time;
                if (!wasGrounded)
                {
                    Landed(-VerticalSpeed);
                }
                VerticalSpeed = Mathf.Max(VerticalSpeed, -4f);
                if (VerticalSpeed < 0f)
                {
                    VerticalSpeed = -4f;
                }
                if (slideQueued)
                {
                    slideQueued = false;
                    StartSlide();
                }
            }
            else if ((flags & CollisionFlags.Above) != 0 && VerticalSpeed > 0f)
            {
                VerticalSpeed = 0f;
            }

            CheckStall(transform.position.z - startZ, Speed * deltaTime);
            SetDust(IsGrounded ? (IsSliding ? 45f : 14f) : 0f);

            if (transform.position.y < fallLimit)
            {
                Runner?.PlayerFell();
            }
        }

        private void ChangeLane(int direction)
        {
            int target = Mathf.Clamp(Lane + direction, -1, 1);
            if (target == Lane)
            {
                return;
            }
            previousLane = Lane;
            Lane = target;
            if (animator != null)
            {
                animator.OnLaneChange(direction);
            }
            Runner?.PlayerChangedLane();
        }

        private void DoJump()
        {
            if (IsSliding)
            {
                if (!HasHeadroom())
                {
                    return;
                }
                EndSlide();
            }
            float height = JumpHeight * (SuperJump ? superJumpMultiplier : 1f);
            VerticalSpeed = Mathf.Sqrt(2f * gravity * height);
            IsGrounded = false;
            lastGroundedTime = -10f;
            jumpRequestTime = -10f;
            slideQueued = false;
            if (animator != null)
            {
                animator.OnJump(SuperJump);
            }
            Runner?.PlayerJumped();
        }

        private void RequestSlide()
        {
            if (IsGrounded)
            {
                StartSlide();
                return;
            }
            // In the air a slide dives down first and starts on landing.
            VerticalSpeed = Mathf.Min(VerticalSpeed, -fastFallSpeed);
            slideQueued = true;
        }

        private void StartSlide()
        {
            IsSliding = true;
            slideTimer = slideDuration;
            SetHeight(slideHeight);
            jumpRequestTime = -10f;
            if (animator != null)
            {
                animator.OnSlide();
            }
            Runner?.PlayerSlid();
        }

        private void EndSlide()
        {
            if (!IsSliding)
            {
                return;
            }
            IsSliding = false;
            SetHeight(standHeight);
        }

        private void SetHeight(float height)
        {
            if (body == null)
            {
                body = GetComponent<CharacterController>();
            }
            body.height = height;
            body.center = new Vector3(0f, height * 0.5f, 0f);
        }

        /// <summary>Whether the runner can stand up here (nothing above a sliding runner).</summary>
        private bool HasHeadroom()
        {
            float radius = body.radius * 0.9f;
            Vector3 feet = transform.position;
            Vector3 bottom = feet + Vector3.up * (slideHeight + 0.05f);
            Vector3 top = feet + Vector3.up * (standHeight - radius);
            return !Physics.CheckCapsule(bottom, top, radius, QueryMask, QueryTriggerInteraction.Ignore);
        }

        private void Landed(float impactSpeed)
        {
            if (animator != null)
            {
                animator.OnLand(impactSpeed);
            }
            Runner?.PlayerLanded(impactSpeed);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!IsRunning)
            {
                return;
            }
            Obstacle obstacle = hit.collider.GetComponentInParent<Obstacle>();
            if (obstacle == null || obstacle.IsKnocked)
            {
                return;
            }
            Vector3 normal = hit.normal;
            if (normal.y > 0.5f)
            {
                return;
            }
            float feet = transform.position.y;
            float top = hit.collider.bounds.max.y;
            bool tall = top > feet + body.stepOffset;
            if (obstacle.IsHazard && tall && normal.z < -0.6f && hit.point.y < top - 0.1f)
            {
                Runner?.PlayerCrashed(obstacle, hit.point);
                return;
            }
            if (tall && Mathf.Abs(normal.x) > 0.6f && bumpCooldown <= 0f && LaneChangeDirection != 0
                && (int)Mathf.Sign(-normal.x) == LaneChangeDirection && hit.point.y > feet + body.stepOffset * 0.5f)
            {
                Runner?.PlayerBumped(obstacle, hit.point);
            }
        }

        /// <summary>
        /// Safety net for contacts the hit test did not classify: a runner that keeps being stopped dead crashes
        /// into whatever is in front of it, or is lifted free when nothing is.
        /// </summary>
        private void CheckStall(float moved, float expected)
        {
            if (expected < 0.02f || moved > expected * 0.25f)
            {
                stallFrames = 0;
                return;
            }
            if (++stallFrames < 4)
            {
                return;
            }
            stallFrames = 0;
            Vector3 center = transform.position + new Vector3(0f, Height * 0.5f, body.radius + 0.3f);
            Collider[] hits = Physics.OverlapBox(center, new Vector3(body.radius, Height * 0.45f, 0.35f), Quaternion.identity, QueryMask, QueryTriggerInteraction.Ignore);
            foreach (Collider collider in hits)
            {
                Obstacle obstacle = collider.GetComponentInParent<Obstacle>();
                if (obstacle != null && !obstacle.IsKnocked)
                {
                    Runner?.PlayerCrashed(obstacle, collider.ClosestPoint(center));
                    return;
                }
            }
            Teleport(transform.position + Vector3.up * 0.4f);
        }

        private void SetDust(float rate)
        {
            if (runDust == null)
            {
                return;
            }
            ParticleSystem.EmissionModule emission = runDust.emission;
            emission.rateOverTimeMultiplier = rate;
        }
    }
}
