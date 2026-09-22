using System.Collections.Generic;
using Gamebox;
using Gamebox.Online;
using TMPro;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// A runner of a race who is played on another device: the look of the runner without its body. It is a copy of
    /// the runner prefab that lost its <see cref="RunnerPlayer"/> and its <see cref="CharacterController"/>, so it
    /// takes no input and touches nothing, tinted in the colour of its seat and with the name of its player over the
    /// head. It moves between the poses that arrive (<see cref="PoseInterpolator"/>), shows the state that travels with
    /// them to the <see cref="RunnerAnimator"/>, and tells the animator when that state changes: a jump, a landing, a
    /// slide, a stumble when a heart is gone, the cheer at the finish.
    /// </summary>
    public class RunnerGhost : PlayerBase, IRunnerMotion
    {
        private const float LabelHeight = 2.25f;
        /// <summary>How much of the seat's colour the model takes; the rest stays its own, so the runner is still to be seen.</summary>
        private const float TintStrength = 0.55f;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private struct Arrival
        {
            public float Time;
            public RunnerPoseState State;
            public int Hearts;
        }

        private readonly PoseInterpolator poses = new PoseInterpolator { SnapDistance = 12f };
        private readonly Queue<Arrival> arrivals = new Queue<Arrival>();
        private RunnerAnimator animator;
        private GameObject shieldBubble;
        private GameObject magnetAura;
        private GameObject superJumpAura;
        private ParticleSystem runDust;
        private RunnerPoseState state;
        private Vector3 velocity;
        private float sideOffset;
        private int hearts = -1;

        public bool IsRunning => state.Running;

        public bool IsGrounded => state.Grounded;

        public bool IsSliding => state.Sliding;

        public bool IsInvulnerable => state.Invulnerable;

        public float Speed => Mathf.Max(0f, velocity.z);

        public float VerticalSpeed => velocity.y;

        public int LaneChangeDirection => state.LaneChange;

        /// <summary>A pose of the runner arrived already.</summary>
        public bool HasPose => poses.HasPose;

        /// <summary>How far the runner is, by the newest pose; the ghost itself plays a moment behind.</summary>
        public float Distance => poses.HasPose ? Mathf.Max(0f, poses.Latest.Position.z) : 0f;

        /// <summary>What the runner is doing, by the newest pose.</summary>
        public RunnerPoseState LatestState => poses.HasPose ? RunnerPoseState.Unpack(poses.Latest.State) : default;

        /// <summary>The hearts the runner has left, or -1 before the first pose.</summary>
        public int Hearts => poses.HasPose ? poses.Latest.Value : -1;

        /// <summary>
        /// Makes a ghost from the runner prefab. The copy is put together asleep, under a holder that is switched off, so
        /// the player and the controller of the prefab are gone before anything of it wakes up.
        /// </summary>
        /// <param name="sideOffset">Shown this far beside where the runner really is, so runners in one lane do not hide each other.</param>
        internal static RunnerGhost Create(GameObject runnerPrefab, Transform parent, string label, Color tint, Material labelMaterial, float sideOffset)
        {
            var holder = new GameObject("Ghost holder");
            holder.SetActive(false);
            GameObject copy = Instantiate(runnerPrefab, holder.transform);
            copy.name = $"Ghost {label}";
            var ghost = copy.AddComponent<RunnerGhost>();
            ghost.sideOffset = sideOffset;
            var player = copy.GetComponent<RunnerPlayer>();
            if (player != null)
            {
                ghost.Adopt(player);
                DestroyImmediate(player);
            }
            foreach (Collider body in copy.GetComponentsInChildren<Collider>(true))
            {
                DestroyImmediate(body);
            }
            ghost.Tint(tint);
            ghost.AddLabel(label, tint, labelMaterial);
            copy.transform.SetParent(parent, false);
            Destroy(holder);
            return ghost;
        }

        /// <summary>Stands the ghost where its runner starts, until the first pose says where it really is.</summary>
        internal void PlaceAt(Vector3 position)
        {
            if (!poses.HasPose)
            {
                transform.position = position + Vector3.right * sideOffset;
            }
        }

        /// <summary>A pose of the runner arrived.</summary>
        internal void Receive(RoomPoseInfo pose)
        {
            float now = Time.unscaledTime;
            poses.Add(pose, now);
            arrivals.Enqueue(new Arrival { Time = now, State = RunnerPoseState.Unpack(pose.State), Hearts = pose.Value });
        }

        /// <summary>What the player of the prefab was wired to is the ghost's from now on.</summary>
        private void Adopt(RunnerPlayer player)
        {
            animator = player.animator;
            shieldBubble = player.shieldBubble;
            magnetAura = player.magnetAura;
            superJumpAura = player.superJumpAura;
            runDust = player.runDust;
            if (animator != null)
            {
                animator.Follow(this);
            }
        }

        private void Tint(Color tint)
        {
            Transform model = animator != null && animator.body != null ? animator.body : transform;
            var block = new MaterialPropertyBlock();
            foreach (Renderer part in model.GetComponentsInChildren<Renderer>(true))
            {
                part.GetPropertyBlock(block);
                block.SetColor(BaseColor, Color.Lerp(Color.white, tint, TintStrength));
                part.SetPropertyBlock(block);
            }
        }

        private void AddLabel(string text, Color tint, Material material)
        {
            var labelObject = new GameObject("Name");
            labelObject.layer = gameObject.layer;
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, LabelHeight, 0f);
            var label = labelObject.AddComponent<TextMeshPro>();
            if (material != null)
            {
                label.fontSharedMaterial = material;
            }
            label.text = text;
            label.fontSize = 2.3f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.Lerp(tint, Color.white, 0.35f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.rectTransform.sizeDelta = new Vector2(8f, 1f);
            labelObject.AddComponent<Billboard>().pulse = 0f;
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            // The state plays as late as the position does, so a jump shows when the ghost leaves the ground.
            while (arrivals.Count > 0 && arrivals.Peek().Time <= now - poses.Delay)
            {
                Apply(arrivals.Dequeue());
            }
            if (poses.Sample(now, out Vector3 position, out Vector3 sampledVelocity, out _))
            {
                position.x += sideOffset;
                transform.position = position;
                velocity = sampledVelocity;
            }
            SetDust(state.Running && state.Grounded ? (state.Sliding ? 45f : 14f) : 0f);
        }

        /// <summary>The next state of the runner: what changed becomes an event of the animator.</summary>
        private void Apply(Arrival arrival)
        {
            RunnerPoseState previous = state;
            state = arrival.State;
            if (animator != null)
            {
                if (previous.Done && !state.Done)
                {
                    animator.ResetPose();
                }
                if (state.Launched && !previous.Launched)
                {
                    animator.Flip();
                }
                else if (previous.Grounded && !state.Grounded && state.Running)
                {
                    animator.OnJump(state.SuperJump);
                }
                if (!previous.Grounded && state.Grounded)
                {
                    animator.OnLand(Mathf.Max(0f, -velocity.y));
                }
                if (state.Sliding && !previous.Sliding)
                {
                    animator.OnSlide();
                }
                if (state.LaneChange != 0 && state.LaneChange != previous.LaneChange)
                {
                    animator.OnLaneChange(state.LaneChange);
                }
                if (hearts >= 0 && arrival.Hearts < hearts && !state.Dead)
                {
                    animator.Stumble();
                }
                if (state.Finished && !previous.Finished)
                {
                    animator.Celebrate();
                }
                if (state.Dead && !previous.Dead)
                {
                    animator.Die();
                }
            }
            hearts = arrival.Hearts;
            SetActive(shieldBubble, state.Shield);
            SetActive(magnetAura, state.Magnet);
            SetActive(superJumpAura, state.SuperJump);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
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
