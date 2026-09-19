using System;
using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The ship. Flies through <see cref="PlayerSimulation"/> (thrust, drift, brake, dash), fires the current weapon of
    /// <see cref="Weapons"/>, sets off nova bombs, and carries a hull and a shield that soaks up hits first and slowly
    /// recharges half-way after a few quiet seconds. Pickups repair it, charge the shield, upgrade the gun, add bombs
    /// and switch on timed power-ups. Its <see cref="AsteroidsGameManager"/> steps it while a mission runs and decides
    /// what happens when it is destroyed.
    /// </summary>
    public class AsteroidsPlayer : PlayerBase, ILocalTransformAdapter
    {
        public const int MaxBombs = 3;
        private const float ShieldRegenDelay = 5f;
        private const float ShieldRegenRate = 7f;

        public Vector3 LocalPosition { get => transform.position; set => transform.position = value; }
        public Quaternion LocalRotation { get => transform.rotation; set => transform.rotation = value; }
        public Vector3 Forward => transform.up;

        [field: SerializeField]
        public PlayerSettings PlayerSettings { get; private set; }

        [SerializeField] internal ShipVisuals visuals;
        [SerializeField] internal Drone[] drones = new Drone[0];
        [SerializeField] internal float novaDamage = 8f;
        [SerializeField] internal float novaBossDamage = 18f;

        public delegate void PointsChanged(int points);
        public delegate void HealthChanged(float health);

        public event PointsChanged PointsChangedEvent;
        public event HealthChanged HealthChangedEvent;
        public event Action<float> ShieldChanged;
        public event Action<int> BombsChanged;
        public event Action<PowerUpType, bool> PowerUpChanged;
        public event Action<DamageInfo> Damaged;
        public event Action<AsteroidsPlayer> Destroyed;
        public event Action<PointReward> CrystalCollected;
        public event Action LifeAwarded;
        public event Action<int> VolleyFired;
        public event Action NovaFired;
        public event Action Dashed;

        private readonly float[] powerUps = new float[PowerUps.Count];
        private readonly List<Barrel> barrels = new List<Barrel>(12);
        private PlayerSimulation simulation;
        private IShipInput input;
        private ShipTouchControls touchControls;
        private int points;
        private float health = 100f;
        private float shield;
        private float fireCooldown;
        private float hurtCooldown;
        private float invulnerable;
        private float regenDelay;
        private bool destroyed;

        public int Points
        {
            get => points;
            set
            {
                points = value;
                PointsChangedEvent?.Invoke(points);
            }
        }

        /// <summary>Hull points.</summary>
        public float Health
        {
            get => health;
            set
            {
                health = Mathf.Min(value, MaxHealth > 0f ? MaxHealth : value);
                HealthChangedEvent?.Invoke(health);
            }
        }

        public float MaxHealth { get; private set; } = 100f;

        public float Shield
        {
            get => shield;
            private set
            {
                shield = Mathf.Clamp(value, 0f, MaxShield);
                ShieldChanged?.Invoke(shield);
            }
        }

        public float MaxShield => PlayerSettings != null ? PlayerSettings.ShieldCapacity : 100f;

        public int Bombs { get; private set; }

        public ShipWeapons Weapons { get; } = new ShipWeapons();

        public PlayerSimulation Simulation => simulation ??= new PlayerSimulation(this, PlayerSettings);

        /// <summary>Where the commands come from: the player's controls unless something else (the autopilot) flies.</summary>
        public IShipInput Input
        {
            get => input ??= new PlayerShipInput { Touch = touchControls };
            set => input = value;
        }

        /// <summary>The on-screen controls of phones and tablets, read by the player's controls.</summary>
        public ShipTouchControls TouchControls
        {
            get => touchControls;
            set
            {
                touchControls = value;
                if (input is PlayerShipInput player)
                {
                    player.Touch = value;
                }
            }
        }

        internal SpaceField Field { get; set; }

        public bool IsAlive => !destroyed && health > 0f && gameObject.activeInHierarchy;

        public bool IsDashing => simulation != null && simulation.IsDashing;

        public bool IsInvulnerable => invulnerable > 0f || IsDashing;

        public float InvulnerableTime => invulnerable;

        public float Radius => PlayerSettings != null ? PlayerSettings.HitRadius : 0.6f;

        public float PickupRadius => Radius + 0.45f;

        public Vector2 Position
        {
            get
            {
                Vector3 position = transform.position;
                return new Vector2(position.x, position.y);
            }
            set => transform.position = new Vector3(value.x, value.y, 0f);
        }

        public Vector2 Velocity => simulation != null ? (Vector2)simulation.Velocity : Vector2.zero;

        private AsteroidsGameManager Asteroids => GameManager as AsteroidsGameManager;


        protected override void Start()
        {
            base.Start();
            simulation ??= new PlayerSimulation(this, PlayerSettings);
        }


        /// <summary>Switches to another ship of the hangar: flight values, toughness and looks.</summary>
        public void ApplyHull(PlayerSettings hull)
        {
            if (hull == null)
            {
                return;
            }
            PlayerSettings = hull;
            Simulation.Apply(hull);
            if (visuals != null)
            {
                visuals.SetModel(hull);
            }
        }


        /// <summary>Kept from the original: a fresh ship with the default hull strength.</summary>
        public void ResetForNewGame()
        {
            ResetForMission(MaxHealth > 0f ? MaxHealth : 100f);
        }


        /// <summary>A fresh ship for a new mission, in the middle of the playfield, with <paramref name="hullStrength"/> hull points.</summary>
        public void ResetForMission(float hullStrength)
        {
            float multiplier = PlayerSettings != null ? PlayerSettings.HullMultiplier : 1f;
            MaxHealth = Mathf.Max(1f, hullStrength * multiplier);
            destroyed = false;
            gameObject.SetActive(true);
            Health = MaxHealth;
            Shield = MaxShield * 0.5f;
            Points = 0;
            Weapons.Reset();
            SetBombs(1);
            ClearPowerUps();
            Position = Vector2.zero;
            transform.rotation = Quaternion.identity;
            Simulation.Stop();
            fireCooldown = 0f;
            hurtCooldown = 0f;
            regenDelay = 0f;
            invulnerable = 1.5f;
            visuals?.ResetVisuals();
        }


        /// <summary>A replacement ship after one was lost: full hull, half a shield, one weapon level less.</summary>
        public void Respawn(Vector2 position)
        {
            destroyed = false;
            gameObject.SetActive(true);
            Health = MaxHealth;
            Shield = MaxShield * 0.5f;
            Weapons.Downgrade();
            ClearPowerUps();
            Position = position;
            transform.rotation = Quaternion.identity;
            Simulation.Stop();
            fireCooldown = 0.4f;
            hurtCooldown = 0f;
            invulnerable = 3f;
            visuals?.ResetVisuals();
            Field?.Effects?.WarpIn(position, 1.8f, PlayerSettings != null ? PlayerSettings.EngineColor : Color.cyan);
            Field?.Sounds?.Respawn();
        }


        /// <summary>One frame of flight and combat. The manager calls it while a mission runs.</summary>
        public void Simulate(float deltaTime, bool controls = true)
        {
            if (!IsAlive)
            {
                return;
            }
            PlayerSimulation flight = Simulation;
            IShipInput commands = Input;
            if (controls)
            {
                commands.Read(this, deltaTime);
            }
            invulnerable = Mathf.Max(0f, invulnerable - deltaTime);
            hurtCooldown = Mathf.Max(0f, hurtCooldown - deltaTime);
            fireCooldown -= deltaTime;
            TickPowerUps(deltaTime);
            RegenerateShield(deltaTime);

            float turn = controls ? commands.Turn : 0f;
            float thrust = controls ? commands.Thrust : 0f;
            flight.Steer(turn, deltaTime);
            if (thrust > 0f)
            {
                flight.Thrust(deltaTime, thrust);
            }
            if (controls && commands.Brake)
            {
                flight.Brake(deltaTime);
            }
            if (controls && commands.DashPressed && flight.Dash())
            {
                Field?.Effects?.DashTrail(Position, Forward, PlayerSettings != null ? PlayerSettings.EngineColor : Color.cyan);
                Field?.Sounds?.Dash();
                Dashed?.Invoke();
            }
            flight.Step(deltaTime);
            if (Field != null && Field.Playground != null)
            {
                Position = Field.Playground.Wrap(Position, Radius);
            }
            if (controls && commands.Fire && fireCooldown <= 0f)
            {
                FireVolley();
            }
            if (controls && commands.BombPressed)
            {
                FireNova();
            }
            foreach (Drone drone in drones)
            {
                if (drone != null && drone.isActiveAndEnabled)
                {
                    drone.Tick(this, deltaTime);
                }
            }
            if (visuals != null)
            {
                visuals.Animate(this, thrust, turn, deltaTime);
            }
        }


        // ------------------------------------------------------------------ weapons

        private void FireVolley()
        {
            SpawnService spawner = Field != null ? Field.Spawner : null;
            if (spawner == null)
            {
                return;
            }
            WeaponType type = Weapons.Type;
            int level = Weapons.Level;
            float rate = (PlayerSettings != null ? PlayerSettings.FireRateMultiplier : 1f) * (IsPowerUpActive(PowerUpType.Overdrive) ? 2f : 1f);
            fireCooldown = WeaponRules.Cooldown(type, level) / Mathf.Max(0.1f, rate);
            WeaponRules.Volley(type, level, barrels);
            Vector2 forward = Forward;
            var right = new Vector2(forward.y, -forward.x);
            Vector2 gun = PlayerSettings != null ? PlayerSettings.GunPoint : new Vector2(0f, 0.8f);
            Vector2 origin = Position + forward * gun.y + right * gun.x;
            int fired = 0;
            foreach (Barrel barrel in barrels)
            {
                Vector2 direction = Quaternion.Euler(0f, 0f, -barrel.Angle) * forward;
                if (spawner.FirePlayerShot(type, level, origin + right * barrel.Offset, direction, Velocity, this) != null)
                {
                    fired++;
                }
            }
            if (fired == 0)
            {
                return;
            }
            visuals?.MuzzleFlash(WeaponRules.Tint(type));
            Field.Sounds?.Fire(type);
            VolleyFired?.Invoke(fired);
        }


        private void FireNova()
        {
            if (Bombs <= 0 || Field == null)
            {
                Field?.Sounds?.Denied();
                return;
            }
            SetBombs(Bombs - 1);
            Field.Nova(Position, novaDamage, novaBossDamage);
            Field.Effects?.Nova(Position);
            Field.Sounds?.Nova();
            Field.CameraRig?.Shake(0.8f);
            Field.CameraRig?.Pulse(1f);
            invulnerable = Mathf.Max(invulnerable, 0.6f);
            NovaFired?.Invoke();
        }


        // ------------------------------------------------------------------ damage

        /// <summary>
        /// Applies <paramref name="hit"/> to the shield and then the hull. Hits right after another are ignored unless
        /// <paramref name="continuous"/> (a black hole's core). Returns false when the ship could not be hurt.
        /// </summary>
        public bool TakeDamage(DamageInfo hit, bool continuous = false)
        {
            if (!IsAlive || IsInvulnerable || (!continuous && hurtCooldown > 0f) || hit.Amount <= 0f)
            {
                return false;
            }
            float amount = hit.Amount;
            if (Shield > 0f)
            {
                float absorbed = Mathf.Min(Shield, amount);
                Shield -= absorbed;
                amount -= absorbed;
                visuals?.ShieldHit(hit.Direction);
                Field?.Sounds?.ShieldHit(Shield <= 0f);
            }
            if (amount > 0f)
            {
                Health -= amount;
                Field?.Effects?.Spark(Position - hit.Direction * Radius, new Color(1f, 0.6f, 0.3f), 1.4f);
                Field?.Sounds?.HullHit();
            }
            hurtCooldown = continuous ? 0f : 0.45f;
            regenDelay = ShieldRegenDelay;
            Damaged?.Invoke(hit);
            if (Health <= 0f)
            {
                Explode();
            }
            return true;
        }


        private void Explode()
        {
            if (destroyed)
            {
                return;
            }
            destroyed = true;
            health = 0f;
            HealthChangedEvent?.Invoke(0f);
            Field?.Effects?.ShipExplosion(Position, PlayerSettings != null ? PlayerSettings.EngineColor : Color.cyan);
            Field?.Sounds?.ShipExplode();
            Field?.CameraRig?.Shake(1f);
            ClearPowerUps();
            gameObject.SetActive(false);
            Destroyed?.Invoke(this);
        }


        /// <summary>Kept from the original: a hit by something shootable destroys the ship outright.</summary>
        public void OnTriggerEnter(Collider other)
        {
            if (other != null && other.GetComponent<Shootable>() != null && GameManager != null && GameManager.IsGameRunning)
            {
                TakeDamage(new DamageInfo(MaxHealth + MaxShield, Vector2.up, Position, DamageSource.Collision, false));
            }
        }


        public void Push(Vector2 impulse)
        {
            Simulation.Push(impulse);
        }


        private void RegenerateShield(float deltaTime)
        {
            regenDelay -= deltaTime;
            float cap = MaxShield * 0.5f;
            if (regenDelay > 0f || Shield >= cap)
            {
                return;
            }
            Shield = Mathf.Min(cap, Shield + ShieldRegenRate * deltaTime);
        }


        // ------------------------------------------------------------------ pickups

        /// <summary>A crystal (points and progress toward a collection objective).</summary>
        public void AwardPoints(PointReward reward)
        {
            Points += reward.PointsAward;
            CrystalCollected?.Invoke(reward);
            if (CrystalCollected == null)
            {
                Asteroids?.IncreaseScore(reward.PointsAward);
            }
        }


        public void AwardHealth(HealthReward reward)
        {
            Repair(reward.HealthAward);
        }


        public void Repair(float amount)
        {
            Health = Mathf.Min(MaxHealth, Health + amount);
        }


        public void RestoreShield(float amount)
        {
            Shield += amount;
        }


        public void AddBomb()
        {
            SetBombs(Bombs + 1);
        }


        public void AwardLife()
        {
            LifeAwarded?.Invoke();
        }


        private void SetBombs(int count)
        {
            Bombs = Mathf.Clamp(count, 0, MaxBombs);
            BombsChanged?.Invoke(Bombs);
        }


        public void ActivatePowerUp(PowerUpType type, float duration)
        {
            bool wasActive = IsPowerUpActive(type);
            powerUps[(int)type] = Mathf.Max(powerUps[(int)type], duration);
            if (type == PowerUpType.Drones)
            {
                SetDrones(true);
            }
            if (!wasActive)
            {
                PowerUpChanged?.Invoke(type, true);
            }
        }


        public bool IsPowerUpActive(PowerUpType type)
        {
            return powerUps[(int)type] > 0f;
        }


        /// <summary>Seconds left on a power-up.</summary>
        public float PowerUpTime(PowerUpType type)
        {
            return powerUps[(int)type];
        }


        private void TickPowerUps(float deltaTime)
        {
            for (int i = 0; i < powerUps.Length; i++)
            {
                if (powerUps[i] <= 0f)
                {
                    continue;
                }
                powerUps[i] -= deltaTime;
                if (powerUps[i] <= 0f)
                {
                    powerUps[i] = 0f;
                    var type = (PowerUpType)i;
                    if (type == PowerUpType.Drones)
                    {
                        SetDrones(false);
                    }
                    PowerUpChanged?.Invoke(type, false);
                }
            }
        }


        private void ClearPowerUps()
        {
            for (int i = 0; i < powerUps.Length; i++)
            {
                if (powerUps[i] > 0f)
                {
                    powerUps[i] = 0f;
                    PowerUpChanged?.Invoke((PowerUpType)i, false);
                }
            }
            SetDrones(false);
        }


        private void SetDrones(bool active)
        {
            for (int i = 0; i < drones.Length; i++)
            {
                if (drones[i] != null)
                {
                    drones[i].SetActive(active, i, drones.Length);
                }
            }
        }
    }
}
