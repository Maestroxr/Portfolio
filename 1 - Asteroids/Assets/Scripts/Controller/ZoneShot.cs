using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A lightning strike (<see cref="EnemyShotKind.Lightning"/>): it stands where it was called. A ring shows the spot
    /// for <see cref="warningTime"/>, then the bolt comes down and burns every ship inside the ring once (a dash or a
    /// quick move gets out of it). It never stops on a ship; every client burns its own ships with it, and the simulator
    /// the freighter or station it lands on.
    /// </summary>
    public class ZoneShot : Shot, IBurnsAllies
    {
        [SerializeField] internal float warningTime = 0.8f;
        [SerializeField] internal float strikeTime = 0.25f;
        [Tooltip("The ring that warns where the strike lands.")]
        [SerializeField] internal Transform ring;
        [Tooltip("The bolt itself, shown while the strike burns.")]
        [SerializeField] internal Transform bolt;
        [SerializeField] internal Light flashLight;
        [SerializeField] internal AudioClip strikeClip;

        private readonly List<object> struck = new List<object>();
        private bool striking;

        internal override bool StopsOnHit => false;

        /// <summary>Whether the bolt is down right now.</summary>
        public bool IsStriking => striking;


        protected override void Awake()
        {
            base.Awake();
            alignToVelocity = false;
        }


        public override void OnSpawned()
        {
            if (lifetime <= 0f)
            {
                lifetime = warningTime + strikeTime + 0.15f;
            }
            base.OnSpawned();
            struck.Clear();
            striking = false;
            Velocity = Vector2.zero;
            transform.rotation = Quaternion.identity;
            if (bolt != null)
            {
                bolt.gameObject.SetActive(false);
            }
            if (flashLight != null)
            {
                flashLight.enabled = false;
            }
        }


        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (!InPlay)
            {
                return;
            }
            bool down = Age >= warningTime && Age < warningTime + strikeTime;
            if (down && !striking)
            {
                striking = true;
                if (bolt != null)
                {
                    bolt.gameObject.SetActive(true);
                    bolt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f));
                }
                if (flashLight != null)
                {
                    flashLight.enabled = true;
                }
                Field?.Sounds?.Play(strikeClip, 0.55f, Random.Range(0.9f, 1.1f), 0.05f, 0.05f);
                Field?.Effects?.Spark(Position, impactTint, radius);
                Field?.CameraRig?.Shake(0.15f);
            }
            else if (!down && striking)
            {
                striking = false;
                if (bolt != null)
                {
                    bolt.gameObject.SetActive(false);
                }
                if (flashLight != null)
                {
                    flashLight.enabled = false;
                }
            }
            if (ring != null)
            {
                float warning = Mathf.Clamp01(Age / Mathf.Max(0.05f, warningTime));
                float size = radius * 2f * (striking ? 1.1f : 1.6f - 0.6f * warning);
                ring.localScale = new Vector3(size, size, 1f);
                ring.gameObject.SetActive(Age < warningTime + strikeTime);
            }
            if (!striking || Field == null)
            {
                return;
            }
            for (int i = 0; i < Field.LocalShipCount; i++)
            {
                AsteroidsPlayer ship = Field.LocalShip(i);
                if (ship == null || !ship.IsAlive || struck.Contains(ship) || !Touches(ship.Position, ship.Radius))
                {
                    continue;
                }
                struck.Add(ship);
                Vector2 away = (ship.Position - Position).sqrMagnitude > 0.001f ? (ship.Position - Position).normalized : Vector2.up;
                ship.TakeDamage(new DamageInfo(damage, away, Position, DamageSource.Enemy, false, this));
            }
        }


        /// <summary>The ring the strike burns; nothing while it only warns.</summary>
        internal override bool Touches(Vector2 center, float size)
        {
            float reach = radius + size;
            return striking && (center - Position).sqrMagnitude <= reach * reach;
        }


        /// <summary>A freighter or station inside the ring takes the strike once.</summary>
        public float Burn(SpaceBody target, float deltaTime)
        {
            if (!striking || struck.Contains(target) || !Touches(target.Position, target.Radius))
            {
                return 0f;
            }
            struck.Add(target);
            return damage;
        }


        protected override void OnDespawned()
        {
            base.OnDespawned();
            struck.Clear();
            striking = false;
        }
    }
}
