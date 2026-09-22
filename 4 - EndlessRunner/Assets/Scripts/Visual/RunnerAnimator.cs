using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// Procedural animation of the runner's jointed model. Blends a run cycle, a jump tuck, a slide, an idle with a
    /// wave, a victory dance and a fall from the runner's state, adds leaning, flips and stumbles on top, and blinks
    /// the model while the runner is invulnerable. The state comes from an <see cref="IRunnerMotion"/>: the player of the
    /// prefab, or whoever else is given with <see cref="Follow"/> (the ghost of a runner played on another device).
    /// </summary>
    public class RunnerAnimator : MonoBehaviour
    {
        private enum Mood
        {
            Idle,
            Run,
            Celebrate,
            Dead
        }

        private struct Pose
        {
            public Vector3 LegLeft, LegRight, ShinLeft, ShinRight;
            public Vector3 ArmLeft, ArmRight, ForearmLeft, ForearmRight;
            public Vector3 Spine, Head, Hips;
            public float HipsHeight;

            public static Pose Lerp(Pose a, Pose b, float t)
            {
                return new Pose
                {
                    LegLeft = Vector3.LerpUnclamped(a.LegLeft, b.LegLeft, t),
                    LegRight = Vector3.LerpUnclamped(a.LegRight, b.LegRight, t),
                    ShinLeft = Vector3.LerpUnclamped(a.ShinLeft, b.ShinLeft, t),
                    ShinRight = Vector3.LerpUnclamped(a.ShinRight, b.ShinRight, t),
                    ArmLeft = Vector3.LerpUnclamped(a.ArmLeft, b.ArmLeft, t),
                    ArmRight = Vector3.LerpUnclamped(a.ArmRight, b.ArmRight, t),
                    ForearmLeft = Vector3.LerpUnclamped(a.ForearmLeft, b.ForearmLeft, t),
                    ForearmRight = Vector3.LerpUnclamped(a.ForearmRight, b.ForearmRight, t),
                    Spine = Vector3.LerpUnclamped(a.Spine, b.Spine, t),
                    Head = Vector3.LerpUnclamped(a.Head, b.Head, t),
                    Hips = Vector3.LerpUnclamped(a.Hips, b.Hips, t),
                    HipsHeight = Mathf.LerpUnclamped(a.HipsHeight, b.HipsHeight, t)
                };
            }
        }

        [SerializeField] internal RunnerPlayer player;
        [Tooltip("Root of the model; leaning, flips and slides rotate it.")]
        [SerializeField] internal Transform body;
        [SerializeField] internal Transform hips;
        [SerializeField] internal Transform spine;
        [SerializeField] internal Transform head;
        [SerializeField] internal Transform armLeft;
        [SerializeField] internal Transform armRight;
        [SerializeField] internal Transform forearmLeft;
        [SerializeField] internal Transform forearmRight;
        [SerializeField] internal Transform legLeft;
        [SerializeField] internal Transform legRight;
        [SerializeField] internal Transform shinLeft;
        [SerializeField] internal Transform shinRight;
        [Tooltip("Meters covered by one full run cycle (two steps).")]
        [SerializeField] internal float strideLength = 3.2f;
        [SerializeField] internal float flipDuration = 0.95f;

        private IRunnerMotion motion;
        private Renderer[] renderers;
        private Vector3 hipsRest;
        private Mood mood = Mood.Idle;
        private float moodTime;
        private float phase;
        private float runWeight;
        private float airWeight;
        private float slideWeight;
        private float lean;
        private float yaw;
        private float flipTime = -1f;
        private bool superJump;
        private float stumble;
        private float landSquash;
        private bool visible = true;

        private void Awake()
        {
            if (body == null)
            {
                body = transform;
            }
            renderers = body.GetComponentsInChildren<Renderer>(true);
            if (hips != null)
            {
                hipsRest = hips.localPosition;
            }
            if (motion == null && player != null)
            {
                motion = player;
            }
        }

        /// <summary>Animates <paramref name="runner"/> from now on, instead of the player the prefab is wired to.</summary>
        internal void Follow(IRunnerMotion runner)
        {
            motion = runner;
        }

        public void ResetPose()
        {
            mood = Mood.Idle;
            moodTime = 0f;
            runWeight = 0f;
            airWeight = 0f;
            slideWeight = 0f;
            lean = 0f;
            yaw = 0f;
            flipTime = -1f;
            stumble = 0f;
            landSquash = 0f;
            SetVisible(true);
        }

        public void OnJump(bool super)
        {
            superJump = super;
            if (super)
            {
                flipTime = 0f;
            }
        }

        public void Flip()
        {
            flipTime = 0f;
        }

        public void OnLand(float impactSpeed)
        {
            landSquash = Mathf.Clamp01(impactSpeed / 18f);
            flipTime = -1f;
            superJump = false;
        }

        public void OnSlide()
        {
            flipTime = -1f;
        }

        public void OnLaneChange(int direction)
        {
            stumble = Mathf.Max(stumble, 0f);
        }

        public void Stumble()
        {
            stumble = 1f;
        }

        public void Celebrate()
        {
            mood = Mood.Celebrate;
            moodTime = 0f;
        }

        public void Die()
        {
            mood = Mood.Dead;
            moodTime = 0f;
        }

        private void LateUpdate()
        {
            if (motion == null || hips == null)
            {
                return;
            }
            float deltaTime = Time.deltaTime;
            moodTime += deltaTime;
            if (mood != Mood.Celebrate && mood != Mood.Dead)
            {
                mood = motion.IsRunning ? Mood.Run : Mood.Idle;
            }

            bool running = mood == Mood.Run;
            float blend = 1f - Mathf.Exp(-12f * deltaTime);
            runWeight = Mathf.Lerp(runWeight, running ? 1f : 0f, blend);
            airWeight = Mathf.Lerp(airWeight, running && !motion.IsGrounded ? 1f : 0f, 1f - Mathf.Exp(-16f * deltaTime));
            slideWeight = Mathf.Lerp(slideWeight, running && motion.IsSliding ? 1f : 0f, 1f - Mathf.Exp(-20f * deltaTime));
            phase += motion.Speed * deltaTime / Mathf.Max(0.5f, strideLength) * Mathf.PI * 2f;

            Pose pose = IdlePose(moodTime);
            pose = Pose.Lerp(pose, RunPose(phase), runWeight);
            pose = Pose.Lerp(pose, AirPose(motion.VerticalSpeed), airWeight);
            pose = Pose.Lerp(pose, SlidePose(), slideWeight);
            if (mood == Mood.Celebrate)
            {
                pose = Pose.Lerp(pose, CelebratePose(moodTime), Mathf.Clamp01(moodTime * 4f));
            }
            else if (mood == Mood.Dead)
            {
                pose = Pose.Lerp(pose, DeadPose(), Mathf.Clamp01(moodTime * 5f));
            }
            landSquash = Mathf.MoveTowards(landSquash, 0f, deltaTime * 4f);
            pose.HipsHeight -= landSquash * 0.12f;
            Apply(pose);
            ApplyBody(deltaTime);
            UpdateBlink();
        }

        private static Pose IdlePose(float time)
        {
            float breathe = Mathf.Sin(time * 2.1f);
            // A friendly wave every few seconds.
            float cycle = time % 5f;
            float wave = cycle > 3f && cycle < 4.6f ? Mathf.Sin((cycle - 3f) / 1.6f * Mathf.PI) : 0f;
            float waggle = Mathf.Sin(time * 14f) * 25f * wave;
            return new Pose
            {
                LegLeft = new Vector3(0f, 0f, -3f),
                LegRight = new Vector3(0f, 0f, 3f),
                ArmLeft = new Vector3(breathe * 3f, 0f, -8f),
                ArmRight = new Vector3(Mathf.Lerp(breathe * -3f, -160f, wave), 0f, Mathf.Lerp(8f, 25f, wave)),
                ForearmLeft = new Vector3(-12f, 0f, 0f),
                ForearmRight = new Vector3(Mathf.Lerp(-12f, -30f, wave), 0f, waggle),
                Spine = new Vector3(2f + breathe * 1.5f, 0f, 0f),
                Head = new Vector3(-2f, Mathf.Sin(time * 0.7f) * 18f, Mathf.Sin(time * 1.3f) * 4f),
                HipsHeight = breathe * 0.008f
            };
        }

        private static Pose RunPose(float phase)
        {
            float s = Mathf.Sin(phase);
            float c = Mathf.Cos(phase);
            return new Pose
            {
                LegLeft = new Vector3(-s * 44f, 0f, 0f),
                LegRight = new Vector3(s * 44f, 0f, 0f),
                ShinLeft = new Vector3(12f + 75f * Mathf.Max(0f, c), 0f, 0f),
                ShinRight = new Vector3(12f + 75f * Mathf.Max(0f, -c), 0f, 0f),
                ArmLeft = new Vector3(s * 40f, 0f, -10f),
                ArmRight = new Vector3(-s * 40f, 0f, 10f),
                ForearmLeft = new Vector3(-80f, 0f, 0f),
                ForearmRight = new Vector3(-80f, 0f, 0f),
                Spine = new Vector3(12f + Mathf.Abs(s) * 3f, s * 9f, 0f),
                Head = new Vector3(-8f, -s * 5f, 0f),
                Hips = new Vector3(0f, -s * 7f, 0f),
                HipsHeight = 0.045f * Mathf.Abs(c) - 0.02f
            };
        }

        private static Pose AirPose(float verticalSpeed)
        {
            float falling = Mathf.Clamp01(-verticalSpeed / 10f);
            return new Pose
            {
                LegLeft = new Vector3(Mathf.Lerp(-65f, -30f, falling), 0f, -4f),
                LegRight = new Vector3(Mathf.Lerp(-15f, -20f, falling), 0f, 4f),
                ShinLeft = new Vector3(Mathf.Lerp(95f, 30f, falling), 0f, 0f),
                ShinRight = new Vector3(Mathf.Lerp(70f, 25f, falling), 0f, 0f),
                ArmLeft = new Vector3(-150f, 0f, -25f),
                ArmRight = new Vector3(-140f, 0f, 25f),
                ForearmLeft = new Vector3(-25f, 0f, 0f),
                ForearmRight = new Vector3(-25f, 0f, 0f),
                Spine = new Vector3(6f, 0f, 0f),
                Head = new Vector3(-10f, 0f, 0f),
                HipsHeight = 0.02f
            };
        }

        private static Pose SlidePose()
        {
            return new Pose
            {
                LegLeft = new Vector3(-55f, 0f, -6f),
                LegRight = new Vector3(-35f, 0f, 6f),
                ShinLeft = new Vector3(8f, 0f, 0f),
                ShinRight = new Vector3(40f, 0f, 0f),
                ArmLeft = new Vector3(40f, 0f, -35f),
                ArmRight = new Vector3(40f, 0f, 35f),
                ForearmLeft = new Vector3(-20f, 0f, 0f),
                ForearmRight = new Vector3(-20f, 0f, 0f),
                Spine = new Vector3(-8f, 0f, 0f),
                Head = new Vector3(45f, 0f, 0f),
                HipsHeight = -0.05f
            };
        }

        private static Pose CelebratePose(float time)
        {
            float hop = Mathf.Abs(Mathf.Sin(time * 6f));
            float wave = Mathf.Sin(time * 12f);
            return new Pose
            {
                LegLeft = new Vector3(-10f * hop, 0f, -6f),
                LegRight = new Vector3(-10f * hop, 0f, 6f),
                ShinLeft = new Vector3(25f * hop, 0f, 0f),
                ShinRight = new Vector3(25f * hop, 0f, 0f),
                ArmLeft = new Vector3(-165f, 0f, -30f + wave * 15f),
                ArmRight = new Vector3(-165f, 0f, 30f - wave * 15f),
                ForearmLeft = new Vector3(-15f, 0f, wave * 20f),
                ForearmRight = new Vector3(-15f, 0f, -wave * 20f),
                Spine = new Vector3(-6f, 0f, wave * 4f),
                Head = new Vector3(-15f, wave * 10f, 0f),
                HipsHeight = hop * 0.25f
            };
        }

        private static Pose DeadPose()
        {
            return new Pose
            {
                LegLeft = new Vector3(-10f, 0f, -18f),
                LegRight = new Vector3(-25f, 0f, 14f),
                ShinLeft = new Vector3(20f, 0f, 0f),
                ShinRight = new Vector3(35f, 0f, 0f),
                ArmLeft = new Vector3(-20f, 0f, -80f),
                ArmRight = new Vector3(-40f, 0f, 85f),
                ForearmLeft = new Vector3(-30f, 0f, 0f),
                ForearmRight = new Vector3(-10f, 0f, 0f),
                Spine = new Vector3(-5f, 0f, 0f),
                Head = new Vector3(-20f, 25f, 0f),
                HipsHeight = 0f
            };
        }

        private void Apply(Pose pose)
        {
            Set(legLeft, pose.LegLeft);
            Set(legRight, pose.LegRight);
            Set(shinLeft, pose.ShinLeft);
            Set(shinRight, pose.ShinRight);
            Set(armLeft, pose.ArmLeft);
            Set(armRight, pose.ArmRight);
            Set(forearmLeft, pose.ForearmLeft);
            Set(forearmRight, pose.ForearmRight);
            Set(spine, pose.Spine);
            Set(head, pose.Head);
            Set(hips, pose.Hips);
            hips.localPosition = hipsRest + Vector3.up * pose.HipsHeight;
        }

        private static void Set(Transform joint, Vector3 euler)
        {
            if (joint != null)
            {
                joint.localRotation = Quaternion.Euler(euler);
            }
        }

        /// <summary>Leaning, facing, flips, slides and falls rotate the whole model around a pivot at the hips.</summary>
        private void ApplyBody(float deltaTime)
        {
            float leanTarget = mood == Mood.Run ? -motion.LaneChangeDirection * 14f : 0f;
            lean = Mathf.Lerp(lean, leanTarget, 1f - Mathf.Exp(-10f * deltaTime));
            float yawTarget = mood == Mood.Run ? motion.LaneChangeDirection * 12f : 0f;
            if (mood == Mood.Celebrate)
            {
                yawTarget = 180f;
            }
            yaw = Mathf.Lerp(yaw, yawTarget, 1f - Mathf.Exp(-(mood == Mood.Celebrate ? 4f : 10f) * deltaTime));

            float pitch = -62f * slideWeight;
            if (flipTime >= 0f)
            {
                flipTime += deltaTime;
                float duration = superJump ? flipDuration * 0.9f : flipDuration;
                float t = Mathf.Clamp01(flipTime / duration);
                pitch += 360f * t * t * (3f - 2f * t);
                if (t >= 1f)
                {
                    flipTime = -1f;
                }
            }
            if (mood == Mood.Dead)
            {
                pitch = Mathf.Lerp(pitch, -85f, Mathf.Clamp01(moodTime * 4f));
            }
            stumble = Mathf.MoveTowards(stumble, 0f, deltaTime * 3f);
            pitch += Mathf.Sin(stumble * 30f) * stumble * 12f;

            Quaternion rotation = Quaternion.Euler(pitch, yaw, lean);
            float pivotHeight = mood == Mood.Dead || slideWeight > 0.01f ? Mathf.Lerp(0.9f, 0.15f, Mathf.Max(slideWeight, mood == Mood.Dead ? 1f : 0f)) : 0.9f;
            Vector3 pivot = Vector3.up * pivotHeight;
            body.localRotation = rotation;
            body.localPosition = pivot - rotation * pivot;
        }

        private void UpdateBlink()
        {
            bool show = !motion.IsInvulnerable || mood != Mood.Run || Mathf.Repeat(Time.time, 0.16f) < 0.1f;
            if (show != visible)
            {
                SetVisible(show);
            }
        }

        private void SetVisible(bool show)
        {
            visible = show;
            if (renderers == null)
            {
                return;
            }
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = show;
                }
            }
        }
    }
}
