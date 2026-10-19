using System;
using Gamebox;
using Gamebox.Racing;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// One car of a race: who drives it, what it is (its loadout and stats), its parts (body, health, weapons, view,
    /// sound) and how its race goes (its racer). Spawned by <see cref="CarFactory"/>.
    ///
    /// The timing of a car's input: <see cref="Update"/> asks the driver once a frame (a person's key presses only exist
    /// per rendered frame) and keeps the answer in <see cref="Input"/>, holding a pressed mine or rocket until the next
    /// physics step has used it; <see cref="FixedUpdate"/> then hands the input to the body and the weapons and moves the
    /// racer's lap tracker along. Before the green light the body is held still (the engine can still be revved); once
    /// the car has finished its weapons fall silent; a wrecked car hears no more from its driver.
    /// </summary>
    public class Car : PlayerBase
    {
        private float shownHealth = -1f;
        private float trackHint = -1f;

        public RaceEntrant Entrant { get; private set; }
        public Race Race { get; private set; }
        public CarStats Stats { get; private set; }
        public CarLoadout Loadout => Entrant?.Loadout;
        public CarBody Body { get; private set; }
        public CarHealth Health { get; private set; }
        public CarWeapons Weapons { get; private set; }
        public CarModelView View { get; private set; }
        public CarAudio Audio { get; private set; }
        public Racer Racer { get; private set; }
        public ICarDriver Driver { get; set; }

        /// <summary>The input of the last frame, held (the pressed actions latched) until the physics step uses it.</summary>
        public CarInput Input { get; private set; }

        public bool IsHuman => Entrant != null && Entrant.Driver == DriverKind.Human;
        public bool IsWrecked { get; private set; }
        public Color Color => Entrant != null ? Entrant.Color : Color.white;

        /// <summary>Whether the car crossed the line for the last time.</summary>
        public bool HasFinished => Racer != null && Racer.Finished;

        /// <summary>Whether the weapons answer the driver (not before the green light, not after the finish, not wrecked).</summary>
        public bool WeaponsLive => !IsWrecked && !HasFinished && Race != null && Race.IsRunning;

        /// <summary>Forward speed in metres per second (negative in reverse).</summary>
        public float Speed => Body != null ? Body.ForwardSpeed : 0f;

        public Vector2 Position2D => new Vector2(transform.position.x, transform.position.z);
        public Vector2 Forward2D => new Vector2(transform.forward.x, transform.forward.z).normalized;

        /// <summary>
        /// Where the car is along the track's centre line (metres), as of the last physics step. Followed by the car itself,
        /// so it goes on after the finish (where the lap tracker stops).
        /// </summary>
        public float TrackDistance => trackHint >= 0f ? trackHint : Racer != null && Racer.Tracker != null ? Racer.Tracker.Distance : 0f;

        /// <summary>How far the car is off the centre line (+ = left), as of the last physics step.</summary>
        public float TrackOffset { get; private set; }

        /// <summary>The car was wrecked (the info of the hit that did it).</summary>
        public event Action<Car, DamageInfo> Wrecked;

        /// <summary>Something happened that the driver should hear of (a kill, a pickup, a lap...).</summary>
        public event Action<CarEvent> Happened;

        /// <summary>Called by the factory once the parts are on the car.</summary>
        public void Setup(RaceEntrant entrant, Race race, ICarDriver driver, CarBody body, CarHealth health, CarWeapons weapons, CarModelView view)
        {
            Entrant = entrant;
            Race = race;
            Driver = driver;
            Body = body;
            Health = health;
            Weapons = weapons;
            View = view;
            Stats = entrant.Loadout.Stats();
            IsWrecked = false;
            Input = CarInput.Idle;
        }

        /// <summary>The car's sound (the factory adds it after the other parts).</summary>
        public void AttachAudio(CarAudio audio)
        {
            Audio = audio;
        }

        public void AttachRacer(Racer racer)
        {
            Racer = racer;
        }

        /// <summary>Tells the car's driver (the HUD, the sounds) what happened.</summary>
        public void Raise(CarEvent happened)
        {
            Happened?.Invoke(happened);
        }

        /// <summary>The car's health ran out (CarHealth calls it): it explodes and is out of the race.</summary>
        public virtual void Wreck(DamageInfo cause)
        {
            if (IsWrecked)
            {
                return;
            }
            IsWrecked = true;
            Input = CarInput.Idle;
            Body?.Wreck();
            if (Weapons != null)
            {
                Weapons.Disarm();
            }
            if (View != null)
            {
                View.SetWrecked(true);
                View.SetBraking(false);
                View.SetNitro(false);
            }
            Audio?.Wreck();
            Wrecked?.Invoke(this, cause);
        }

        /// <summary>Asks the driver what to do this frame and keeps it for the physics step.</summary>
        protected virtual void Update()
        {
            if (Race == null || Time.timeScale <= 0f)
            {
                return;
            }
            CarInput fresh = CarInput.Idle;
            if (!IsWrecked && Driver != null && Race.Phase != RacePhase.Grid)
            {
                fresh = Driver.Drive(this, Time.deltaTime);
            }
            // A press waits for the physics step (several frames can pass between two steps).
            CarInput held = Input;
            fresh.DropMine |= held.DropMine;
            fresh.FireRocket |= held.FireRocket;
            Input = fresh;
        }

        /// <summary>The body and the weapons act on the input; the lap tracker follows the car.</summary>
        protected virtual void FixedUpdate()
        {
            if (Body == null || Race == null)
            {
                return;
            }
            float deltaTime = Time.fixedDeltaTime;
            CarInput input = Input;
            Body.Held = Race.Phase == RacePhase.Grid || Race.Phase == RacePhase.Countdown;
            FollowTrack();
            Body.Step(input, deltaTime);
            if (Weapons != null && !IsWrecked && !Body.Held)
            {
                Weapons.Tick(WeaponsLive ? input : Unarmed(input), deltaTime);
            }
            if (Racer != null && Racer.Tracker != null && Race.IsRunning)
            {
                Racer.Tracker.Update(PhysicsPosition2D, Race.Time);
            }
            if (input.DropMine || input.FireRocket)
            {
                input.DropMine = false;
                input.FireRocket = false;
                Input = input;
            }
        }

        /// <summary>The wheels, the lights and the damage of the model follow the body.</summary>
        protected virtual void LateUpdate()
        {
            if (View == null || Body == null)
            {
                return;
            }
            View.SetWheels(Body.SteerAngle, Body.WheelSpin);
            if (!IsWrecked)
            {
                View.SetBraking(Body.Braking);
                View.SetNitro(Body.Nitro);
            }
            float health = Health != null ? Health.Health01 : 1f;
            if (Mathf.Abs(health - shownHealth) > 0.01f)
            {
                shownHealth = health;
                View.SetDamage(health);
            }
        }

        /// <summary>The car's position in the ground plane as of the last physics step (not the interpolated one).</summary>
        public Vector2 PhysicsPosition2D
        {
            get
            {
                Vector3 position = Body != null && Body.Rigidbody != null ? Body.Rigidbody.position : transform.position;
                return new Vector2(position.x, position.z);
            }
        }

        /// <summary>Where the car is against the track: its offset from the centre, and whether it is off the road.</summary>
        private void FollowTrack()
        {
            Track track = Race.Track;
            if (track == null || track.Path == null)
            {
                return;
            }
            RacePath path = track.Path;
            Vector2 position = PhysicsPosition2D;
            float hint = trackHint >= 0f ? trackHint : Racer != null && Racer.Tracker != null ? Racer.Tracker.Distance : -1f;
            float distance = path.Project(position, hint, 30f);
            trackHint = distance;
            TrackOffset = path.SignedOffset(position, distance);
            Body.OffRoad = Mathf.Abs(TrackOffset) > track.HalfWidthAt(distance) + 0.4f;
        }

        private static CarInput Unarmed(CarInput input)
        {
            input.Fire = false;
            input.Nitro = false;
            input.DropMine = false;
            input.FireRocket = false;
            return input;
        }
    }
}
