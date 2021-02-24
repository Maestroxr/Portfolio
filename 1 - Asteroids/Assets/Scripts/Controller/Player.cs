using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio
{
    public class Player : MonoBehaviour, ILocalTransformAdapter, ICyclic<Player>
    {
        public Vector3 LocalPosition { get => transform.position; set => transform.position = value; }
        public Quaternion LocalRotation { get => transform.rotation; set => transform.rotation = value; }
        public Vector3 Forward { get => transform.up; }
        [field: SerializeField]
        public PlayerSettings PlayerSettings { get; private set; }
        public delegate void PointsChanged(int points);
        public delegate void HealthChanged(float health);
        public event PointsChanged PointsChangedEvent;
        public event HealthChanged HealthChangedEvent;
        public int Points { get => points; set { points = value; PointsChangedEvent?.Invoke(points); } }
        public float Health { get => health; set { health = value; HealthChangedEvent(health); } }
        public Player Mirror { get; private set; }
        public bool IsMirror { get; private set; }
        public bool IsOppositeShown { get; set; }
        public ShotPool ShotsPool;

        
        [SerializeField]
        private Shot shotGameObject;
        private float health = 1000;
        private int points = 0;
        private PlayerSimulation playerSimulation;
        private float lifeLose;
        private float timeUntilLifeLoseIncrease;
        private int shotsCount = 0;


        void Start()
        {
            if (IsMirror)
            {
                return;
            }

            playerSimulation = new PlayerSimulation(this, PlayerSettings);
            Health = PlayerSettings.StartingLife;
            Points = 0;
            lifeLose = PlayerSettings.LifeLosePerSecond;
            timeUntilLifeLoseIncrease = PlayerSettings.LifeLoseIncreaseTime;
        }


        void Update()
        {
            if (!IsMirror && Health > 0)
            {
                var deltaTime = Time.deltaTime;
                if (Input.GetKey(PlayerSettings.Forward))
                {
                    playerSimulation.MoveForward(deltaTime);
                }
                if (Input.GetKey(PlayerSettings.Left))
                {
                    playerSimulation.Rotate(Vector3.forward, deltaTime);
                }
                else if (Input.GetKey(PlayerSettings.Right))
                {
                    playerSimulation.Rotate(Vector3.back, deltaTime);
                }

                playerSimulation.UpdateRotation(deltaTime);

                if (Input.GetKeyDown(PlayerSettings.Shoot))
                {
                    Shoot();
                }

                timeUntilLifeLoseIncrease -= deltaTime;
                if (timeUntilLifeLoseIncrease < 0)
                {
                    lifeLose += PlayerSettings.LifeLoseIncreaseRate;
                    timeUntilLifeLoseIncrease += PlayerSettings.LifeLoseIncreaseTime;
                }
                Health -= deltaTime * lifeLose;
            }
        }


        public void Shoot()
        {
            if (!IsMirror)
            {
                Shot shot = ShotsPool.Deploy();
                shot.transform.position = transform.position;
                shot.transform.rotation = transform.rotation;
                shot.FiredBy = this;
                shot.ShotHitEvent += (shotHit, obj) => ShotsPool.Undeploy(shotHit);
                shot.StartTimeAlive();

                Floatable floatingShot = shot.GetComponent<Floatable>();
                floatingShot.Direction = transform.up;

                shotsCount++;
                shot.name = $"Shot {shotsCount}";
            }
        }


        public void OnTriggerEnter(Collider other)
        {
            Shootable willKillMe = other.gameObject.GetComponent<Shootable>();
            if (willKillMe != null)
            {
                // Player Destroyed
                gameObject.SetActive(false);
                if (IsMirror)
                {
                    Mirror.Health = 0;
                    Mirror.gameObject.SetActive(false);
                }
                else
                {
                    Health = 0;
                }
            }
        }


        public void AwardPoints(PointReward reward)
        {
            Points += reward.PointsAward;
        }


        public void AwardHealth(HealthReward reward)
        {
            Health += reward.HealthAward;
        }

        public void Setup(Player mirror, bool isMirror)
        {
            Mirror = mirror;
            IsMirror = isMirror;
        }
    }
}