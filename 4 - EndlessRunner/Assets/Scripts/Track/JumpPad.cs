using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>A bounce pad that launches the runner high into the air.</summary>
    public class JumpPad : Collidable
    {
        [Tooltip("Height of the launch, in meters.")]
        [SerializeField] internal float launchHeight = 5.5f;
        [SerializeField] internal Transform membrane;

        private float squash;

        public float LaunchHeight => launchHeight;

        public override void OnSpawned()
        {
            base.OnSpawned();
            squash = 0f;
            if (membrane != null)
            {
                membrane.localScale = Vector3.one;
            }
        }

        protected override bool CanTouch(RunnerPlayer player)
        {
            // Only a runner on (or just above) the pad bounces; one flying over it does not.
            return player.transform.position.y < transform.position.y + 0.6f;
        }

        protected override void OnTouched(RunnerGameManager manager, RunnerPlayer player)
        {
            squash = 1f;
            manager.Launch(this);
        }

        private void Update()
        {
            if (membrane == null || squash <= 0f)
            {
                return;
            }
            squash = Mathf.Max(0f, squash - Time.deltaTime * 3f);
            float wobble = Mathf.Sin(squash * 20f) * squash;
            membrane.localScale = new Vector3(1f + wobble * 0.15f, 1f - wobble * 0.5f, 1f + wobble * 0.15f);
        }
    }
}
