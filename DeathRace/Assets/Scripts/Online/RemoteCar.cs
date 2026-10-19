using Gamebox.Online;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// A car that a person on another device drives, as this device shows it: a kinematic puppet that moves between the
    /// poses that arrive (<see cref="PoseInterpolator"/>, a little behind, so there are two to move between) through
    /// <see cref="CarBody.Follow"/>, which works out its speed, slide, wheels and brakes from the motion (so its engine,
    /// skid marks and wheels behave as a driven car's do); its nitro flames and its gun fire from the pose's bits
    /// through its <see cref="RemoteDriver"/>. Its health is the one its own device reports (smoke and fire
    /// follow it); nothing hurts it here (<see cref="CarHealth.Authoritative"/> is off), because its device judges every
    /// hit. It is wrecked when its device says so: the room's wreck action names the killer
    /// (<see cref="WreckBy"/>); a pose that says wrecked without the action is believed after a moment. A car over the
    /// line leaves the track once its poses stop. Runs after the car's own physics step, which leaves a kinematic body
    /// alone.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class RemoteCar : MonoBehaviour
    {
        /// <summary>How long a pose may say wrecked before the car is wrecked here without the room's action.</summary>
        private const float WreckWait = 1.2f;

        /// <summary>How long after its last pose a car over the line leaves the track here.</summary>
        private const float FinishedWait = 1f;

        private readonly PoseInterpolator poses = new PoseInterpolator { Delay = 0.12f, MaxExtrapolation = 0.3f, SnapDistance = 12f };
        private float wreckedSince = -1f;
        private bool posed;

        public Car Car { get; private set; }
        public RemoteDriver Driver { get; private set; }

        /// <summary>The seat of the car's driver in the room.</summary>
        public byte RoomSeat { get; private set; }

        /// <summary>The health its device reported last, 0 to 1.</summary>
        public float ReportedHealth { get; private set; } = 1f;

        /// <summary>When the last pose arrived (unscaled time), or -1.</summary>
        public float LastPoseTime { get; private set; } = -1f;

        /// <summary>Makes <paramref name="car"/> the puppet of the player at <paramref name="roomSeat"/>.</summary>
        public static RemoteCar Attach(Car car, RemoteDriver driver, byte roomSeat)
        {
            RemoteCar remote = car.gameObject.AddComponent<RemoteCar>();
            remote.Car = car;
            remote.Driver = driver;
            remote.RoomSeat = roomSeat;
            if (car.Health != null)
            {
                car.Health.Authoritative = false;
            }
            if (car.Body != null && car.Body.Rigidbody != null)
            {
                car.Body.Rigidbody.isKinematic = true;
                car.Body.Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
            return remote;
        }

        /// <summary>A pose of the car arrived.</summary>
        public void Add(RoomPoseInfo pose)
        {
            poses.Add(pose, Time.unscaledTime);
            LastPoseTime = Time.unscaledTime;
            if (Driver != null)
            {
                Driver.State = pose.State;
            }
            ReportedHealth = OnlineRaceRules.UnpackHealth(pose.Value);
            if (Car == null || Car.IsWrecked || Car.Health == null)
            {
                return;
            }
            // The wreck itself waits for the room's action (it names the killer): until then the car keeps a sliver.
            Car.Health.ShowHealth(Mathf.Max(ReportedHealth, 0.01f));
            if (OnlineRaceRules.Has(pose.State, OnlineRaceRules.WreckedBit))
            {
                if (wreckedSince < 0f)
                {
                    wreckedSince = Time.unscaledTime;
                }
            }
            else
            {
                wreckedSince = -1f;
            }
        }

        /// <summary>Its device says the car was wrecked, by <paramref name="killer"/> (null: by nobody).</summary>
        public void WreckBy(Car killer)
        {
            if (Car != null && !Car.IsWrecked && Car.Health != null)
            {
                Car.Health.ShowHealth(0f, killer);
            }
        }

        private void FixedUpdate()
        {
            if (Car == null || Car.Body == null || Car.Body.Rigidbody == null)
            {
                return;
            }
            Rigidbody body = Car.Body.Rigidbody;
            if (!body.isKinematic)
            {
                body.isKinematic = true;
            }
            if (poses.Sample(Time.unscaledTime, out Vector3 position, out Vector3 velocity, out float heading))
            {
                position.y = 0f;
                Quaternion rotation = Quaternion.Euler(0f, heading, 0f);
                if (!posed)
                {
                    // The first pose: put the car there rather than slide it from the grid slot the director gave it.
                    posed = true;
                    body.position = position;
                    body.rotation = rotation;
                    transform.SetPositionAndRotation(position, rotation);
                }
                // The body takes the pose and works out its speed, slide, wheel and brakes from it, as the model shows them.
                Car.Body.Follow(position, rotation, velocity, Driver != null && Driver.Braking, Time.fixedDeltaTime);
            }
            KeepArmed();
            if (wreckedSince >= 0f && Time.unscaledTime - wreckedSince > WreckWait)
            {
                wreckedSince = -1f;
                WreckBy(null);
            }
            if ((Car.HasFinished || Driver != null && Driver.Finished) && !Car.IsWrecked && LastPoseTime >= 0f && Time.unscaledTime - LastPoseTime > FinishedWait)
            {
                // Its device stopped sending once it reported its finish (the server keeps no poses of a racer who is done):
                // rather than stand frozen past the line in everybody's way, the car leaves the track here.
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// The car's stock here is only for show (its device counts the real one): the gun never runs dry while its driver
        /// fires, and a nitro charge is there whenever the driver burns one.
        /// </summary>
        private void KeepArmed()
        {
            CarWeapons weapons = Car.Weapons;
            if (weapons == null || Car.IsWrecked)
            {
                return;
            }
            if (weapons.Ammo < weapons.AmmoCapacity / 4)
            {
                weapons.AddAmmo(weapons.AmmoCapacity);
            }
            if (Driver != null && Driver.Nitro && weapons.NitroCharges == 0 && !weapons.NitroActive)
            {
                weapons.AddNitro(1);
            }
        }
    }
}
