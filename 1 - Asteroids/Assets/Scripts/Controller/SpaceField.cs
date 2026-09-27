using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>An explosion: everything shootable within its radius takes damage and is pushed away.</summary>
    public struct Blast
    {
        public Vector2 Center;
        public float Radius;
        /// <summary>Damage to asteroids, mines and enemies at the centre; half of it at the rim.</summary>
        public float Damage;
        /// <summary>Hull damage to the ship at the centre; zero when the blast cannot hurt the ship.</summary>
        public float PlayerDamage;
        public float Push;
        public bool ByPlayer;
        /// <summary>In a shared mission: the seat of the pilot on another device who set it off; null for the local ship or nobody.</summary>
        public int? Seat;
        public SpaceBody Source;
        public Color Tint;
        /// <summary>The layers the blast hurts (strike); None means both, so the blasts of the asteroid field hurt everything.</summary>
        public Altitude Layers;
    }


    /// <summary>
    /// The simulated playfield. Keeps every body in play, moves them each frame of a running mission (everything but
    /// the ship's own shots slows down under a chrono field), and resolves what touches what with circle tests in the
    /// plane: the ship's shots against asteroids, mines and enemies, enemy fire against the ship, the ship against
    /// everything it can fly into and every pickup it can grab. Explosions are queued and resolved in order, so chain
    /// reactions ripple outward without recursion. Bodies added or removed while the field is busy take effect when
    /// it is done, so a rock's fragments are never hit by the shot that broke it.
    ///
    /// In a mission shared with other pilots the field has a <see cref="Link"/>. Every client flies its own ship against
    /// its own copy of the world and decides what hurts that ship; one client simulates the world, and on the others
    /// the bodies are puppets (<see cref="IsReplica"/>). The ships of the other pilots are stand-ins that take no damage
    /// here, but enemies, mines and pickups go for the closest ship of them all (<see cref="NearestShip"/>).
    /// </summary>
    public class SpaceField : MonoBehaviour
    {
        [SerializeField] internal Playground playground;
        [SerializeField] internal SpawnService spawner;
        [SerializeField] internal SpaceEffects effects;
        [SerializeField] internal AsteroidsAudio sounds;
        [SerializeField] internal CameraRig cameraRig;
        [Tooltip("Explosions resolved per frame at most, which bounds a chain reaction's work.")]
        [SerializeField] internal int maxBlastsPerFrame = 120;
        [Tooltip("The scrolling ground of the strike missions.")]
        [SerializeField] internal StrikeTerrain terrain;

        private readonly List<SpaceBody> bodies = new List<SpaceBody>();
        private readonly List<SpaceBody> added = new List<SpaceBody>();
        private readonly List<SpaceBody> removed = new List<SpaceBody>();
        private readonly List<Shootable> targets = new List<Shootable>();
        private readonly List<Shot> playerShots = new List<Shot>();
        private readonly List<Shot> enemyShots = new List<Shot>();
        private readonly List<Reward> rewards = new List<Reward>();
        private readonly List<GravityWell> wells = new List<GravityWell>();
        private readonly List<Comet> comets = new List<Comet>();
        private readonly Queue<Blast> blasts = new Queue<Blast>();
        private readonly List<AsteroidsPlayer> ships = new List<AsteroidsPlayer>();
        private readonly List<Shootable> candidates = new List<Shootable>();
        private int busy;

        public Playground Playground => playground;

        /// <summary>The scrolling ground of the strike missions; null in a scene without it.</summary>
        public StrikeTerrain Terrain => terrain;

        /// <summary>Meters per second the ground scrolls down in a strike mission; zero otherwise.</summary>
        public float ScrollSpeed { get; set; }

        /// <summary>The motion of the ground: what a parked ground unit or a pickup lying on it moves by.</summary>
        public Vector2 ScrollVelocity => new Vector2(0f, -ScrollSpeed);

        /// <summary>How far the ground has scrolled in the current strike mission (m).</summary>
        public float ScrollDistance { get; set; }

        /// <summary>
        /// Range (m) from which every pickup drifts to the nearest ship, when larger than the pickup's own (the free flight
        /// after a strike boss dies: <see cref="StrikeRules.EndPickupPull"/>); zero for none.
        /// </summary>
        public float PickupPull { get; set; }
        public SpawnService Spawner => spawner;
        public SpaceEffects Effects => effects;
        public AsteroidsAudio Sounds => sounds;
        public CameraRig CameraRig => cameraRig;

        /// <summary>The ship; null while there is none.</summary>
        public AsteroidsPlayer Player { get; set; }

        /// <summary>The stand-ins of the other pilots' ships in a shared mission; empty otherwise.</summary>
        public IReadOnlyList<AsteroidsPlayer> RemoteShips => ships;

        /// <summary>Keeps the copies of a shared playfield in step; null in the single player game.</summary>
        public IFieldLink Link { get; set; }

        /// <summary>The bodies of this field are puppets of a world another pilot's client simulates.</summary>
        public bool IsReplica => Link != null && !Link.Simulates;

        /// <summary>Speed of time for everything but the ship and its shots (below 1 under a chrono field).</summary>
        public float WorldTimeScale { get; set; } = 1f;

        public IReadOnlyList<SpaceBody> Bodies => bodies;
        public IReadOnlyList<Shootable> Targets => targets;
        public IReadOnlyList<Shot> PlayerShots => playerShots;
        public IReadOnlyList<Shot> EnemyShots => enemyShots;
        public IReadOnlyList<Reward> Rewards => rewards;
        public IReadOnlyList<GravityWell> Wells => wells;
        public IReadOnlyList<Comet> Comets => comets;

        public bool IsBusy => busy > 0;

        /// <summary>A shootable took damage and survived or not.</summary>
        public event Action<Shootable, DamageInfo> TargetHit;

        /// <summary>A shootable was destroyed.</summary>
        public event Action<Shootable, DamageInfo> TargetDestroyed;

        /// <summary>The ship grabbed a pickup.</summary>
        public event Action<Reward> RewardCollected;

        /// <summary>A blast went off (before it hurts anything).</summary>
        public event Action<Blast> Exploded;

        /// <summary>One of the ship's shots hit something for the first time.</summary>
        public event Action<Shot> ShotLanded;

        /// <summary>A hostile of a strike mission (an aircraft or a ground unit) came on screen for the first time.</summary>
        public event Action<SpaceBody> HostileEntered;


        private bool PlayerAlive => Player != null && Player.IsAlive;


        private void Awake()
        {
            if (effects != null)
            {
                effects.Field = this;
            }
        }


        // ------------------------------------------------------------------ bodies

        /// <summary>Puts <paramref name="body"/> into play (at the end of the current step when the field is busy).</summary>
        public void Add(SpaceBody body)
        {
            if (body == null)
            {
                return;
            }
            body.Field = this;
            body.InPlay = true;
            body.OnSpawned();
            if (IsBusy)
            {
                added.Add(body);
            }
            else
            {
                Register(body);
            }
            Link?.BodyAdded(body);
        }


        /// <summary>Takes <paramref name="body"/> out of play and returns it to its pool.</summary>
        public void Remove(SpaceBody body)
        {
            if (body == null || !body.InPlay)
            {
                return;
            }
            body.InPlay = false;
            Link?.BodyRemoved(body);
            if (IsBusy)
            {
                removed.Add(body);
            }
            else
            {
                Unregister(body);
                body.Release();
            }
        }


        /// <summary>Removes every body.</summary>
        public void Clear()
        {
            Enter();
            for (int i = 0; i < bodies.Count; i++)
            {
                Remove(bodies[i]);
            }
            for (int i = 0; i < added.Count; i++)
            {
                Remove(added[i]);
            }
            blasts.Clear();
            Exit();
        }


        private void Register(SpaceBody body)
        {
            if (!body.InPlay || bodies.Contains(body))
            {
                return;
            }
            bodies.Add(body);
            switch (body)
            {
                case Shootable shootable:
                    targets.Add(shootable);
                    break;
                case Shot shot:
                    (shot.IsEnemy ? enemyShots : playerShots).Add(shot);
                    break;
                case Reward reward:
                    rewards.Add(reward);
                    break;
                case GravityWell well:
                    wells.Add(well);
                    break;
                case Comet comet:
                    comets.Add(comet);
                    break;
            }
        }


        private void Unregister(SpaceBody body)
        {
            bodies.Remove(body);
            switch (body)
            {
                case Shootable shootable:
                    targets.Remove(shootable);
                    break;
                case Shot shot:
                    playerShots.Remove(shot);
                    enemyShots.Remove(shot);
                    break;
                case Reward reward:
                    rewards.Remove(reward);
                    break;
                case GravityWell well:
                    wells.Remove(well);
                    break;
                case Comet comet:
                    comets.Remove(comet);
                    break;
            }
        }


        private void Enter()
        {
            busy++;
        }


        private void Exit()
        {
            busy--;
            if (busy > 0)
            {
                return;
            }
            busy = 0;
            // Removals first: a body added and removed within the same step never enters the lists.
            for (int i = 0; i < removed.Count; i++)
            {
                Unregister(removed[i]);
                removed[i].Release();
            }
            removed.Clear();
            for (int i = 0; i < added.Count; i++)
            {
                Register(added[i]);
            }
            added.Clear();
        }


        // ------------------------------------------------------------------ simulation

        /// <summary>Moves everything by <paramref name="deltaTime"/> seconds and resolves what collides.</summary>
        public void Tick(float deltaTime)
        {
            Enter();
            float worldDelta = deltaTime * WorldTimeScale;
            for (int i = 0; i < bodies.Count; i++)
            {
                SpaceBody body = bodies[i];
                if (!body.InPlay)
                {
                    continue;
                }
                body.Tick(body is Shot shot && !shot.IsEnemy ? deltaTime : worldDelta);
            }
            ApplyGravity(worldDelta, deltaTime);
            CollidePlayerShots();
            CollideEnemyShots();
            CollidePlayer();
            CollectRewards();
            ProcessBlasts();
            Exit();
        }


        private void ApplyGravity(float worldDelta, float deltaTime)
        {
            for (int w = 0; w < wells.Count; w++)
            {
                GravityWell well = wells[w];
                if (!well.InPlay)
                {
                    continue;
                }
                for (int i = 0; i < bodies.Count; i++)
                {
                    SpaceBody body = bodies[i];
                    if (!body.InPlay || !body.PulledByGravity || body == well)
                    {
                        continue;
                    }
                    body.Velocity += well.Pull(body.Position) * worldDelta;
                    // What a black hole swallows is for the simulator to say.
                    if (!IsReplica && well.Swallows(body))
                    {
                        well.Consume(body);
                    }
                }
                if (PlayerAlive)
                {
                    Player.Push(well.Pull(Player.Position) * deltaTime * 0.8f);
                    well.AffectPlayer(Player, deltaTime);
                }
            }
        }


        private void CollidePlayerShots()
        {
            for (int s = 0; s < playerShots.Count; s++)
            {
                Shot shot = playerShots[s];
                if (!shot.InPlay)
                {
                    continue;
                }
                Vector2 shotPosition = shot.Position;
                for (int t = 0; t < targets.Count; t++)
                {
                    Shootable target = targets[t];
                    if (target == null || !target.IsAlive)
                    {
                        continue;
                    }
                    // The cheap distance test first: whether a strike target is on screen is asked only of the few it touches.
                    float reach = shot.Radius + target.Radius;
                    if ((target.Position - shotPosition).sqrMagnitude > reach * reach || !IsTargetable(target) || !shot.CanHit(target))
                    {
                        continue;
                    }
                    bool first = shot.HitCount == 0 && !shot.IsGhost;
                    shot.Hit(target);
                    if (first)
                    {
                        ShotLanded?.Invoke(shot);
                    }
                    if (!shot.InPlay)
                    {
                        break;
                    }
                }
            }
        }


        private void CollideEnemyShots()
        {
            if (!PlayerAlive)
            {
                return;
            }
            Vector2 shipPosition = Player.Position;
            for (int s = 0; s < enemyShots.Count; s++)
            {
                Shot shot = enemyShots[s];
                if (!shot.InPlay)
                {
                    continue;
                }
                // A shot that does not stop on the ship (a beam) hurts it in its own tick while it touches.
                if (!shot.Touches(shipPosition, Player.Radius) || !shot.StopsOnHit)
                {
                    continue;
                }
                Vector2 direction = shot.Velocity.sqrMagnitude > 0.01f ? shot.Velocity.normalized : (shipPosition - shot.Position).normalized;
                Player.TakeDamage(new DamageInfo(shot.Damage, direction, shot.Position, DamageSource.Enemy, false, shot));
                shot.Impact();
            }
        }


        private void CollidePlayer()
        {
            if (!PlayerAlive)
            {
                return;
            }
            if (playground != null && !playground.Wraps)
            {
                RamStrike();
                return;
            }
            for (int t = 0; t < targets.Count; t++)
            {
                Shootable target = targets[t];
                if (!target.IsAlive || target.ContactDamage <= 0f)
                {
                    continue;
                }
                Vector2 delta = Player.Position - target.Position;
                float reach = Player.Radius + target.Radius * 0.9f;
                if (delta.sqrMagnitude > reach * reach)
                {
                    continue;
                }
                float distance = delta.magnitude;
                Vector2 away = distance > 0.001f ? delta / distance : Vector2.up;
                bool dashing = Player.IsDashing;
                bool hurt = !dashing && Player.TakeDamage(new DamageInfo(target.ContactDamage, away, target.Position + away * target.Radius,
                    DamageSource.Collision, false));
                if ((hurt || dashing) && target.IsPuppet)
                {
                    Link?.PuppetRammed(target, -away, dashing);
                }
                else if (hurt || dashing)
                {
                    target.OnRammed(Player, -away, dashing);
                }
                // Bounce apart so the two do not stay inside each other.
                float overlap = reach - distance;
                Player.Position += away * overlap;
                if (hurt)
                {
                    Player.Push(away * (4f + target.Radius * 2f));
                    target.Velocity -= away * (1.5f / Mathf.Max(0.5f, target.Radius));
                }
                if (!PlayerAlive)
                {
                    return;
                }
            }
            for (int c = 0; c < comets.Count; c++)
            {
                Comet comet = comets[c];
                if (!comet.InPlay)
                {
                    continue;
                }
                float reach = Player.Radius + comet.Radius;
                Vector2 delta = Player.Position - comet.Position;
                if (delta.sqrMagnitude <= reach * reach)
                {
                    comet.HitPlayer(Player);
                    if (!PlayerAlive)
                    {
                        return;
                    }
                }
            }
        }


        /// <summary>
        /// Strike ramming: while the ship overlaps an air unit, every <see cref="StrikeRules.RamInterval"/> the unit takes
        /// <see cref="StrikeRules.RamUnitDamage"/> (by the player) and the ship takes damage by the unit's size. Nothing
        /// bounces or is pushed; ground units are flown over; boss cores and parts hurt the ship but take nothing.
        /// </summary>
        private void RamStrike()
        {
            for (int t = 0; t < targets.Count; t++)
            {
                Shootable target = targets[t];
                if (!target.IsAlive || (target.Altitude & Altitude.Air) == 0)
                {
                    continue;
                }
                Vector2 delta = Player.Position - target.Position;
                float reach = Player.Radius + target.Radius * 0.9f;
                if (delta.sqrMagnitude > reach * reach || !target.TakeRamTurn(StrikeRules.RamInterval))
                {
                    continue;
                }
                float distance = delta.magnitude;
                Vector2 away = distance > 0.001f ? delta / distance : Vector2.down;
                Player.TakeDamage(new DamageInfo(StrikeRules.RamPilotDamage(target.Radius * 2f), away, target.Position + away * target.Radius,
                    DamageSource.Collision, false));
                if (!(target is Boss) && !(target is BossPart))
                {
                    if (target.IsPuppet)
                    {
                        Link?.PuppetRammed(target, -away, false);
                    }
                    else
                    {
                        target.OnRammed(Player, -away, false);
                    }
                }
                if (!PlayerAlive)
                {
                    return;
                }
            }
        }


        private void CollectRewards()
        {
            if (!PlayerAlive)
            {
                return;
            }
            Vector2 shipPosition = Player.Position;
            for (int r = 0; r < rewards.Count; r++)
            {
                Reward reward = rewards[r];
                if (!reward.InPlay || !reward.CanBeCollected)
                {
                    continue;
                }
                float reach = Player.PickupRadius + reward.Radius;
                if ((reward.Position - shipPosition).sqrMagnitude > reach * reach)
                {
                    continue;
                }
                if (Link != null)
                {
                    // Several ships may reach for it: the server gives it to the first, see GrantReward.
                    if (!reward.Claimed)
                    {
                        reward.Claimed = true;
                        Link.RewardTouched(reward);
                    }
                    continue;
                }
                reward.Collect(Player);
                RewardCollected?.Invoke(reward);
            }
        }


        /// <summary>The server gave a pickup of a shared mission to the local ship.</summary>
        public void GrantReward(Reward reward)
        {
            if (reward == null || !reward.InPlay || Player == null)
            {
                return;
            }
            reward.Collect(Player);
            RewardCollected?.Invoke(reward);
        }


        // ------------------------------------------------------------------ explosions

        /// <summary>Sets off a blast; resolved at the end of the current step, or right away when the field is idle.</summary>
        public void Explode(Blast blast)
        {
            blasts.Enqueue(blast);
            if (!IsBusy)
            {
                Enter();
                ProcessBlasts();
                Exit();
            }
        }


        private void ProcessBlasts()
        {
            int processed = 0;
            while (blasts.Count > 0 && processed++ < maxBlastsPerFrame)
            {
                Blast blast = blasts.Dequeue();
                Exploded?.Invoke(blast);
                if (Link != null && Link.Simulates)
                {
                    Link.BlastSetOff(blast);
                }
                Altitude layers = blast.Layers == Altitude.None ? Altitude.Both : blast.Layers;
                // Nothing is pushed in a strike mission: ground units move with the ground, aircraft on their paths.
                float push = playground == null || playground.Wraps ? blast.Push : 0f;
                for (int t = 0; t < targets.Count; t++)
                {
                    Shootable target = targets[t];
                    if (target == null || target == blast.Source || !target.IsAlive || (target.Altitude & layers) == 0)
                    {
                        continue;
                    }
                    Vector2 delta = target.Position - blast.Center;
                    float reach = blast.Radius + target.Radius;
                    if (delta.sqrMagnitude > reach * reach || !IsTargetable(target))
                    {
                        continue;
                    }
                    float distance = delta.magnitude;
                    float falloff = 1f - 0.5f * Mathf.Clamp01(distance / reach);
                    Vector2 direction = distance > 0.001f ? delta / distance : UnityEngine.Random.insideUnitCircle.normalized;
                    target.Velocity += direction * push * falloff / Mathf.Max(0.6f, target.Radius);
                    target.TakeHit(new DamageInfo(blast.Damage * falloff, direction, target.Position - direction * target.Radius,
                        DamageSource.Explosion, blast.ByPlayer) { Seat = blast.Seat });
                }
                HurtPlayer(blast);
            }
            if (blasts.Count > 0 && processed >= maxBlastsPerFrame)
            {
                Debug.LogWarning($"{name}: {blasts.Count} blasts left after {maxBlastsPerFrame} this frame; they go off next frame.", this);
            }
        }


        /// <summary>The part of a blast that is about the ship at this device.</summary>
        private void HurtPlayer(Blast blast)
        {
            if (blast.PlayerDamage <= 0f || !PlayerAlive)
            {
                return;
            }
            Vector2 delta = Player.Position - blast.Center;
            float reach = blast.Radius + Player.Radius;
            if (delta.sqrMagnitude > reach * reach)
            {
                return;
            }
            float distance = delta.magnitude;
            float falloff = 1f - 0.5f * Mathf.Clamp01(distance / reach);
            Vector2 direction = distance > 0.001f ? delta / distance : Vector2.up;
            if (Player.TakeDamage(new DamageInfo(blast.PlayerDamage * falloff, direction, blast.Center, DamageSource.Explosion, false))
                && (playground == null || playground.Wraps))
            {
                Player.Push(direction * blast.Push * falloff);
            }
        }


        /// <summary>
        /// A blast of the simulator's field arrives at a replica: it shows and hurts the ship here. What it did to the
        /// world comes with the bodies.
        /// </summary>
        public void ShowBlast(Blast blast)
        {
            Exploded?.Invoke(blast);
            HurtPlayer(blast);
        }


        /// <summary>
        /// A new ship at <paramref name="center"/> gets a little room: a harmless shockwave pushes what is around it away.
        /// On a replica the simulator is asked to do it.
        /// </summary>
        public void MakeRoom(Vector2 center, int? seat = null)
        {
            if (IsReplica)
            {
                Link.RoomWanted(center);
                return;
            }
            Explode(new Blast { Center = center, Radius = 3.5f, Damage = 0f, PlayerDamage = 0f, Push = 5f, Seat = seat, Tint = new Color(0.4f, 0.8f, 1f) });
        }


        /// <summary>
        /// The ship's nova bomb: a shockwave over the whole playfield that damages everything shootable, wipes out enemy
        /// fire and pushes the rocks away from <paramref name="center"/>. In a shared mission the other clients hear of
        /// the local ship's nova, and the simulator's field takes the damage: there <paramref name="seat"/> names the pilot
        /// on another device who set it off. In a strike mission (the playground does not wrap) it is the megabomb: it hits
        /// only what is on screen and pushes nothing.
        /// </summary>
        public void Nova(Vector2 center, float damage, float bossDamage, int? seat = null)
        {
            if (!seat.HasValue)
            {
                Link?.NovaFired(center, damage, bossDamage);
            }
            if (IsReplica)
            {
                if (playground != null && !playground.Wraps)
                {
                    // A megabomb clears the screen it was set off on at once: the enemy shots here are puppets that would
                    // hurt the ship until the simulator's exits come back. They only go (the simulator clears the real ones).
                    Enter();
                    for (int i = 0; i < enemyShots.Count; i++)
                    {
                        if (enemyShots[i].InPlay)
                        {
                            enemyShots[i].PlayImpact();
                        }
                    }
                    Exit();
                }
                return;
            }
            bool push = playground == null || playground.Wraps;
            Enter();
            for (int i = 0; i < enemyShots.Count; i++)
            {
                if (enemyShots[i].InPlay)
                {
                    enemyShots[i].Impact();
                }
            }
            for (int t = 0; t < targets.Count; t++)
            {
                Shootable target = targets[t];
                if (!IsTargetable(target))
                {
                    continue;
                }
                Vector2 delta = target.Position - center;
                Vector2 direction = delta.sqrMagnitude > 0.001f ? delta.normalized : Vector2.up;
                if (push)
                {
                    target.Velocity += direction * 4f / Mathf.Max(0.6f, target.Radius);
                }
                float amount = target is Boss ? bossDamage : damage;
                target.TakeHit(new DamageInfo(amount, direction, target.Position, DamageSource.Nova, true) { Seat = seat });
            }
            Exit();
        }


        // ------------------------------------------------------------------ ships

        /// <summary>Adds the stand-in of another pilot's ship (a shared mission).</summary>
        public void AddShip(AsteroidsPlayer remote)
        {
            if (remote != null && remote != Player && !ships.Contains(remote))
            {
                ships.Add(remote);
            }
        }


        public void RemoveShip(AsteroidsPlayer remote)
        {
            ships.Remove(remote);
        }


        /// <summary>
        /// The living ship closest to <paramref name="from"/> across the wrapping edges: the one enemies hunt, mines arm for
        /// and pickups drift to. Null when no ship is alive.
        /// </summary>
        public AsteroidsPlayer NearestShip(Vector2 from)
        {
            AsteroidsPlayer best = PlayerAlive ? Player : null;
            if (ships.Count == 0)
            {
                return best;
            }
            float bestDistance = best != null ? ShipDistance(from, best) : float.MaxValue;
            for (int i = 0; i < ships.Count; i++)
            {
                AsteroidsPlayer candidate = ships[i];
                if (candidate == null || !candidate.IsAlive)
                {
                    continue;
                }
                float distance = ShipDistance(from, candidate);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }
            return best;
        }


        /// <summary>Distance from <paramref name="point"/> to the closest living ship; very far when there is none.</summary>
        public float ShipClearance(Vector2 point)
        {
            AsteroidsPlayer nearest = NearestShip(point);
            return nearest != null ? Mathf.Sqrt(ShipDistance(point, nearest)) : float.MaxValue;
        }


        private float ShipDistance(Vector2 from, AsteroidsPlayer ship)
        {
            return (playground != null ? playground.Delta(from, ship.Position) : ship.Position - from).sqrMagnitude;
        }


        // ------------------------------------------------------------------ notifications from bodies

        internal void NotifyHit(Shootable target, DamageInfo hit)
        {
            TargetHit?.Invoke(target, hit);
        }


        /// <summary>
        /// A shootable was destroyed (every kind of target and boss tells the field): it notes whether a pilot did it, which
        /// its exit tells the others, then the listeners hear of it.
        /// </summary>
        internal void NotifyDestroyed(Shootable target, DamageInfo hit)
        {
            target.ExitByPlayer = hit.ByPlayer;
            TargetDestroyed?.Invoke(target, hit);
        }


        /// <summary>A strike hostile came on screen for the first time (the units call it once; counts for the kill star).</summary>
        internal void NoteEntered(SpaceBody body)
        {
            HostileEntered?.Invoke(body);
        }


        // ------------------------------------------------------------------ queries

        /// <summary>The closest living shootable to <paramref name="from"/> within <paramref name="range"/> that passes <paramref name="filter"/>.</summary>
        public Shootable NearestTarget(Vector2 from, float range, Func<Shootable, bool> filter = null)
        {
            Shootable best = null;
            float bestDistance = range * range;
            for (int i = 0; i < targets.Count; i++)
            {
                Shootable target = targets[i];
                if (!IsTargetable(target) || (filter != null && !filter(target)))
                {
                    continue;
                }
                float distance = (target.Position - from).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = target;
                }
            }
            return best;
        }


        /// <summary>
        /// Whether <paramref name="target"/> can be hit: alive, and in a strike mission (the playground does not wrap) at
        /// least half on screen, so nothing is shot before it shows. An armoured piece of a strike boss (its core or a part
        /// of a higher tier) is not a target there either: shots, missiles and beams pass through it to the pieces that
        /// can be hurt, which may sit behind it.
        /// </summary>
        public bool IsTargetable(Shootable target)
        {
            if (target == null || !target.IsAlive)
            {
                return false;
            }
            if (playground == null || playground.Wraps)
            {
                return true;
            }
            if (target.Invulnerable && (target is StrikeBoss || target is BossPart))
            {
                return false;
            }
            return playground.IsInside(target.Position, -target.Radius * 0.5f);
        }


        /// <summary>
        /// A targetable target on a layer of <paramref name="mask"/>, picked by <paramref name="roll"/> (0 to 1) among them
        /// in field order; null when there is none.
        /// </summary>
        public Shootable RandomTarget(Altitude mask, float roll)
        {
            candidates.Clear();
            for (int i = 0; i < targets.Count; i++)
            {
                Shootable target = targets[i];
                if ((target.Altitude & mask) != 0 && IsTargetable(target))
                {
                    candidates.Add(target);
                }
            }
            if (candidates.Count == 0)
            {
                return null;
            }
            int index = Mathf.Clamp(Mathf.FloorToInt(roll * candidates.Count), 0, candidates.Count - 1);
            Shootable picked = candidates[index];
            candidates.Clear();
            return picked;
        }


        /// <summary>
        /// The first targetable target on a layer of <paramref name="mask"/> up the column of half width
        /// <paramref name="halfWidth"/> around <paramref name="x"/> from <paramref name="fromY"/> (a beam's): the one with
        /// the lowest y whose circle meets the column above <paramref name="fromY"/>; null when there is none.
        /// </summary>
        public Shootable FirstInColumn(float x, float halfWidth, float fromY, Altitude mask)
        {
            Shootable best = null;
            float bestY = float.MaxValue;
            for (int i = 0; i < targets.Count; i++)
            {
                Shootable target = targets[i];
                if ((target.Altitude & mask) == 0 || !IsTargetable(target))
                {
                    continue;
                }
                Vector2 position = target.Position;
                if (Mathf.Abs(position.x - x) > halfWidth + target.Radius || position.y + target.Radius < fromY)
                {
                    continue;
                }
                if (position.y < bestY)
                {
                    bestY = position.y;
                    best = target;
                }
            }
            return best;
        }


        /// <summary>Living shootables that count toward clearing the current wave, including ones about to be added.</summary>
        public int WaveTargetsAlive
        {
            get
            {
                int count = 0;
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i].IsAlive && targets[i].CountsForWave)
                    {
                        count++;
                    }
                }
                for (int i = 0; i < added.Count; i++)
                {
                    if (added[i] is Shootable shootable && shootable.InPlay && shootable.CountsForWave)
                    {
                        count++;
                    }
                }
                return count;
            }
        }


        public int CountTargets(Func<Shootable, bool> predicate)
        {
            int count = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].IsAlive && predicate(targets[i]))
                {
                    count++;
                }
            }
            return count;
        }
    }
}
