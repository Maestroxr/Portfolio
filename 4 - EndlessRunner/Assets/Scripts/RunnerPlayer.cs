using Gamebox;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>The runner. Moves forward while the game runs, steers with the arrow keys and ends the run when it falls.</summary>
    public class RunnerPlayer : PlayerBase
    {
        [SerializeField] private float ForwardSpeed;
        [SerializeField] private float SideSpeed;

        private RunnerGameManager Runner => GameManager as RunnerGameManager;


        public void ApplySettings(RunnerSettings settings)
        {
            if (settings == null)
            {
                return;
            }
            ForwardSpeed = settings.ForwardSpeed;
            SideSpeed = settings.SideSpeed;
        }


        /// <summary>Stops any physics motion left from a fall.</summary>
        public void ResetMotion()
        {
            var body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }


        // Update is called once per frame
        void Update()
        {
            if (GameManager == null || !GameManager.IsGameRunning)
            {
                return;
            }

            var moveTo = new Vector3(0, 0, -ForwardSpeed);
            if (Input.GetKey("left"))
            {
                moveTo.x = SideSpeed;
            }
            if (Input.GetKey("right"))
            {
                moveTo.x = -SideSpeed;
            }

            transform.position = Vector3.Lerp(transform.position, transform.position + moveTo, Time.deltaTime);

            if (transform.position.y < -1f)
            {
                Runner?.GameOver();
            }
        }
    }
}
