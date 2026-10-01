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
    ///
    /// In a shared mission the ships of the other pilots are instances of the same prefab that stand in for them
    /// (<see cref="PlayerBase.IsRemote"/>, moved by a <see cref="RemoteShip"/>): they are never simulated and take no
    /// damage here, and only mirror what their owner reports (<see cref="Mirror"/>).
    /// </summary>
    public class AsteroidsPlayer : PlayerBase, ILocalTransformAdapter
    {
        public const int MaxBombs = 3;

        /// <summary>Strike: height of the ship above the bottom edge at the start of a mission (m).</summary>
        public const float StartHeight = 3f;

        /// <summary>Strike: a continuous hit (an enemy beam) destroys at most one weapon in this many seconds.</summary>
        public const float ContinuousWeaponLossInterval = 0.3f;

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
        [Tooltip("The beams and zaps of the strike weapons (found among the children when not set).")]
        [SerializeField] internal PlayerBeam beam;

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
        private IShipInput ownControls;
        private ShipTouchControls touchControls;
        private int points;
        private float health = 100f;
        private float shield;
        private float fireCooldown;
        private float hurtCooldown;
        private float invulnerable;
        private float regenDelay;
        private bool destroyed;
        private StrikeLoadout strikeLoadout;
        private float energyClock;
        private float weaponLossCooldown;
        private bool beamLookedUp;

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

        /// <summary>The shield's capacity: the hull's, or one phase shield (100) in strike.</summary>
        public float MaxShield => Mode == MissionMode.Strike ? StrikeRules.PhaseShieldPoints : PlayerSettings != null ? PlayerSettings.ShieldCapacity : 100f;

        public int Bombs { get; private set; }

        /// <summary>The most bombs the ship carries: nova bombs in the asteroid field (<see cref="MaxBombs"/>), megabombs in strike.</summary>
        public int BombCapacity => Mode == MissionMode.Strike ? StrikeRules.MaxMegabombs : MaxBombs;

        /// <summary>The kind of mission the ship flies (set by the resets for a mission).</summary>
        public MissionMode Mode { get; internal set; }

        /// <summary>The strike weapons of the local ship in a strike mission; null otherwise and on the stand-ins of other pilots.</summary>
        public StrikeGunnery Strike { get; private set; }

        public ShipWeapons Weapons { get; } = new ShipWeapons();

        /// <summary>The beams and zaps of the strike weapons on the ship; null when the prefab has none.</summary>
        public PlayerBeam Beam
        {
            get
            {
                if (beam == null && !beamLookedUp)
                {
                    beamLookedUp = true;
                    beam = GetComponentInChildren<PlayerBeam>(true);
                }
                return beam;
            }
        }

        /// <summary>Strike: seconds the megabomb still cools down (0 when ready).</summary>
        public float MegabombCooldown => Strike != null ? Strike.MegabombCooldown : 0f;

        public PlayerSimulation Simulation => simulation ??= new PlayerSimulation(this, PlayerSettings);

        /// <summary>
        /// Where the commands come from: the pilot's controls (<see cref="OwnControls"/>, else the player's) unless something
        /// else (the autopilot) flies. Null hands the ship back to the pilot's controls.
        /// </summary>
        public IShipInput Input
        {
            get => input ??= ownControls ?? new PlayerShipInput { Touch = touchControls };
            set => input = value;
        }

        /// <summary>
        /// The controls of the pilot of a local co-op mission (<see cref="LocalShipInput"/>), which the ship flies by and
        /// goes back to when the autopilot lets go; null for the player's own controls of the single player game. Setting
        /// it hands the ship to them at once.
        /// </summary>
        internal IShipInput OwnControls
        {
            get => ownControls;
            set
            {
                ownControls = value;
                input = null;
            }
        }

        /// <summary>
        /// A ship of a local co-op mission besides the player's own (<see cref="SpaceField.Wingmen"/>): flown and hurt at this
        /// device like it, with the kills, pickups and blasts of its pilot (<see cref="PlayerBase.Seat"/>) counted for them.
        /// </summary>
        internal bool IsWingman { get; set; }

        /// <summary>
        /// The seat a hit by this ship counts for (<see cref="DamageInfo.Seat"/>): the pilot's of a stand-in of an online room
        /// or of a wingman of local co-op; null for the player's own ship.
        /// </summary>
        internal int? HitSeat => IsRemote || IsWingman ? Seat : (int?)null;

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
            LeaveStrike();
            Mode = MissionMode.Field;
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
            TouchControls?.ShowMode(MissionMode.Field);
        }


        /// <summary>
        /// A fresh ship for a strike mission flying <paramref name="working"/> (the working copy of the pilot): 100 energy at
        /// most, the energy as its hull, the phase shield in use as its shield, the megabombs as its bombs, 1.5 s of
        /// invulnerability, at the bottom centre of the playfield.
        /// </summary>
        public void ResetForStrike(StrikeLoadout working)
        {
            LeaveStrike();
            working ??= StrikeLoadout.NewPilot();
            Mode = MissionMode.Strike;
            Strike = new StrikeGunnery(working);
            strikeLoadout = working;
            working.Changed += SyncFromLoadout;
            MaxHealth = StrikeRules.MaxEnergy;
            destroyed = false;
            gameObject.SetActive(true);
            Points = 0;
            Weapons.Reset();
            ClearPowerUps();
            SyncFromLoadout();
            float bottom = Field != null && Field.Playground != null ? Field.Playground.Middle.y - StrikeRules.HalfSize.y : -StrikeRules.HalfSize.y;
            Position = new Vector2(0f, bottom + StartHeight);
            transform.rotation = Quaternion.identity;
            Simulation.Stop();
            fireCooldown = 0f;
            hurtCooldown = 0f;
            regenDelay = 0f;
            energyClock = 0f;
            weaponLossCooldown = 0f;
            invulnerable = StrikeRules.StartInvulnerability;
            visuals?.ResetVisuals();
            PlayerBeam beams = Beam;
            if (beams != null)
            {
                beams.HideAll();
            }
            TouchControls?.ShowMode(MissionMode.Strike);
        }


        /// <summary>The loadout changed: the hull, shield and bombs mirror it (the loadout is the source of truth in strike).</summary>
        internal void SyncFromLoadout()
        {
            StrikeLoadout loadout = Strike != null ? Strike.Loadout : null;
            if (loadout == null)
            {
                return;
            }
            MaxHealth = StrikeRules.MaxEnergy;
            if (!destroyed)
            {
                Health = Mathf.Clamp(loadout.Energy, 0f, StrikeRules.MaxEnergy);
            }
            Shield = loadout.PhaseShields > 0 ? loadout.ShieldPoints : 0f;
            SetBombs(loadout.Megabombs);
        }


        /// <summary>Drops the strike weapons and stops mirroring the strike loadout (another mission starts).</summary>
        private void LeaveStrike()
        {
            if (strikeLoadout != null)
            {
                strikeLoadout.Changed -= SyncFromLoadout;
                strikeLoadout = null;
            }
            if (Strike != null)
            {
                Strike.Stop(this);
                Strike.Detach();
                Strike = null;
            }
        }


        /// <summary>Strike: the gunnery counts a volley of <paramref name="shots"/> projectiles (the accuracy statistics).</summary>
        internal void NoteVolley(int shots)
        {
            VolleyFired?.Invoke(shots);
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
            if (Mode == MissionMode.Strike)
            {
                SimulateStrike(deltaTime, controls);
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


        /// <summary>
        /// One frame of a strike mission: direct 8-way flight (nose up, clamped to the screen), the strike weapons, the
        /// megabomb and the energy regeneration. During the countdown the ship flies and fires, but the invulnerability of
        /// the mission start waits for GO and the megabomb is not dropped (nothing is on screen yet).
        /// </summary>
        private void SimulateStrike(float deltaTime, bool controls)
        {
            PlayerSimulation flight = Simulation;
            IShipInput commands = Input;
            if (controls)
            {
                commands.Read(this, deltaTime);
            }
            AsteroidsGameManager manager = Asteroids;
            bool briefing = manager != null && manager.IsBriefing;
            if (!briefing)
            {
                invulnerable = Mathf.Max(0f, invulnerable - deltaTime);
            }
            weaponLossCooldown = Mathf.Max(0f, weaponLossCooldown - deltaTime);
            Vector2 move = controls ? commands.Move : Vector2.zero;
            float maxSpeed = StrikeRules.ShipMaxSpeed(PlayerSettings);
            flight.Fly(move, maxSpeed, deltaTime);
            flight.StepFree(deltaTime);
            if (Field != null && Field.Playground != null)
            {
                Vector2 before = Position;
                Vector2 clamped = Field.Playground.Clamp(before, Radius);
                if (clamped != before)
                {
                    // Against an edge the ship stops along it instead of pressing on.
                    Vector3 velocity = flight.Velocity;
                    if (!Mathf.Approximately(clamped.x, before.x))
                    {
                        velocity.x = 0f;
                    }
                    if (!Mathf.Approximately(clamped.y, before.y))
                    {
                        velocity.y = 0f;
                    }
                    flight.Velocity = velocity;
                    Position = clamped;
                }
            }
            transform.rotation = Quaternion.identity;

            StrikeGunnery gunnery = Strike;
            if (gunnery != null)
            {
                if (controls && commands.CyclePressed)
                {
                    gunnery.Cycle();
                }
                gunnery.Update(this, deltaTime, controls && commands.Fire);
                if (!IsAlive)
                {
                    return;
                }
                if (controls && commands.BombPressed && !briefing)
                {
                    gunnery.Megabomb(this);
                }
                RegenerateEnergy(gunnery, deltaTime);
            }
            if (visuals != null)
            {
                visuals.AnimateStrike(this, move, maxSpeed, deltaTime);
            }
            TouchControls?.ShowStrikeState(Bombs, gunnery != null ? gunnery.MegabombCooldown / StrikeRules.MegabombCooldown : 0f);
        }


        /// <summary>
        /// Strike: +1 energy every <see cref="StrikeRules.EnergyRegenInterval"/> seconds without firing (every volley
        /// restarts the wait), never on Elite, never outside the running mission (the fly-off) and not while a boss dies.
        /// </summary>
        private void RegenerateEnergy(StrikeGunnery gunnery, float deltaTime)
        {
            StrikeLoadout loadout = gunnery.Loadout;
            if (loadout == null || gunnery.FiredThisFrame || !CanRegenerateEnergy(loadout))
            {
                energyClock = 0f;
                return;
            }
            energyClock += deltaTime;
            if (energyClock < StrikeRules.EnergyRegenInterval)
            {
                return;
            }
            energyClock -= StrikeRules.EnergyRegenInterval;
            loadout.AddEnergy(StrikeRules.EnergyRegenAmount);
        }


        private bool CanRegenerateEnergy(StrikeLoadout loadout)
        {
            if (!StrikeRules.RegeneratesEnergy(loadout.Difficulty) || loadout.Energy >= StrikeRules.MaxEnergy)
            {
                return false;
            }
            AsteroidsGameManager manager = Asteroids;
            if (manager != null && !manager.IsMissionActive)
            {
                return false;
            }
            return Field == null || !IsBossDying(Field);
        }


        /// <summary>Whether a boss is in its death throes (in play with no hull left).</summary>
        private static bool IsBossDying(SpaceField field)
        {
            IReadOnlyList<Shootable> targets = field.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] is Boss boss && boss.InPlay && boss.Health <= 0f)
                {
                    return true;
                }
            }
            return false;
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
            Field.Nova(Position, novaDamage, novaBossDamage, HitSeat);
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
            if (Mode == MissionMode.Strike)
            {
                return TakeStrikeDamage(hit, continuous);
            }
            // What hurts the ship of another pilot is decided on their device.
            if (!IsAlive || IsRemote || IsInvulnerable || (!continuous && hurtCooldown > 0f) || hit.Amount <= 0f)
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


        /// <summary>
        /// A hit in strike: the difficulty and hull multipliers, the phase shield first, a weapon lost at low energy, the
        /// ship destroyed at 0 energy; no hurt cooldown. Returns false when the ship could not be hurt.
        /// </summary>
        private bool TakeStrikeDamage(DamageInfo hit, bool continuous)
        {
            // What hurts the ship of another pilot is decided on their device. No hurt cooldown in strike: only the
            // invulnerability of the mission start, and the fly-off of a won mission (the input is locked, the pilot
            // cannot dodge, and a loss there would throw the win away).
            StrikeLoadout loadout = Strike != null ? Strike.Loadout : null;
            if (loadout == null || !IsAlive || IsRemote || invulnerable > 0f || hit.Amount <= 0f)
            {
                return false;
            }
            AsteroidsGameManager manager = Asteroids;
            if (manager != null && manager.IsFlyingOff)
            {
                return false;
            }
            float amount = StrikeDamage(hit.Amount, loadout.Difficulty, PlayerSettings != null ? PlayerSettings.HullMultiplier : 1f);
            bool shielded = loadout.PhaseShields > 0;
            int shieldsBefore = loadout.PhaseShields;
            float taken = loadout.AbsorbDamage(amount);
            if (shielded)
            {
                visuals?.ShieldHit(hit.Direction);
                Field?.Sounds?.ShieldHit(loadout.PhaseShields < shieldsBefore);
            }
            else
            {
                Field?.Effects?.Spark(Position - hit.Direction * Radius, new Color(1f, 0.6f, 0.3f), continuous ? 0.6f : 1.4f);
                Field?.Sounds?.HullHit();
            }
            // A hit on the energy (no phase shield took it) that leaves it low also destroys a weapon (the loadout checks
            // the energy); a beam's burn destroys at most one a while.
            if (!shielded && loadout.Energy > 0f && loadout.WeaponsAtRisk && (!continuous || weaponLossCooldown <= 0f))
            {
                weaponLossCooldown = continuous ? ContinuousWeaponLossInterval : 0f;
                StrikeItem lost = loadout.LoseWeapon();
                if (lost != StrikeItem.MachineGun)
                {
                    Field?.Sounds?.WeaponLost();
                    Strike.NotifyWeaponLost(lost);
                }
            }
            if (loadout.PhaseShields <= 0 && loadout.Energy > 0f && loadout.Energy <= StrikeRules.LowEnergy)
            {
                Field?.Sounds?.ShieldLow();
            }
            SyncFromLoadout();
            // The listeners hear the damage actually taken, after the multipliers (the damage star counts it).
            DamageInfo taking = hit;
            taking.Amount = taken;
            Damaged?.Invoke(taking);
            if (loadout.Energy <= 0f)
            {
                Explode();
            }
            return true;
        }


        /// <summary>
        /// Strike: the damage a hit of <paramref name="amount"/> does to the pilot on <paramref name="difficulty"/> with a hull
        /// of <paramref name="hullMultiplier"/> (design 1.1: x0.5 on Rookie, divided by the hull's multiplier).
        /// </summary>
        public static float StrikeDamage(float amount, StrikeDifficulty difficulty, float hullMultiplier)
        {
            return amount * StrikeRules.DamageTaken(difficulty) / Mathf.Max(0.1f, hullMultiplier);
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
            Strike?.Stop(this);
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


        /// <summary>
        /// A stand-in takes over what the pilot's own device reports about the ship: whether it flies, its hull and shield
        /// (which show), the tractor magnet (pickups drift to the ship that has it) and the wing drones.
        /// </summary>
        internal void Mirror(ShipPose pose, float maxHealth)
        {
            MaxHealth = Mathf.Max(1f, maxHealth);
            destroyed = !pose.Alive;
            health = pose.Alive ? Mathf.Max(0.01f, pose.Health * MaxHealth) : 0f;
            shield = Mathf.Clamp(pose.Shield * MaxShield, 0f, MaxShield);
            powerUps[(int)PowerUpType.Magnet] = pose.Magnet ? 1f : 0f;
            powerUps[(int)PowerUpType.Drones] = pose.Drones ? 1f : 0f;
            SetDrones(pose.Drones && pose.Alive);
        }


        /// <summary>The drones of a stand-in circle it (they fire on their owner's device).</summary>
        internal void TickDrones(float deltaTime)
        {
            foreach (Drone drone in drones)
            {
                if (drone != null && drone.isActiveAndEnabled)
                {
                    drone.Tick(this, deltaTime);
                }
            }
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
            Bombs = Mathf.Clamp(count, 0, BombCapacity);
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
