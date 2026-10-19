using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The view of a race from above. Following a car it looks down at a fixed north-up angle (the track never turns
    /// under the player, so its shape can be learned), a little ahead of where the car is going, and climbs a little as
    /// the car speeds up so there is more road to see. The car itself is followed exactly (its Rigidbody interpolates,
    /// so the view is smooth); only the look-ahead and the height ease, which keeps the car steady on screen. The
    /// overview drifts slowly over the leading cars (the computer cars behind the menu), or holds the whole track (the
    /// spare quarter of a three-way split screen). Hits shake it through a <see cref="CameraShake"/>.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class RaceCamera : MonoBehaviour
    {
        /// <summary>The height over the car at the normal distance, metres.</summary>
        public const float BaseHeight = 48f;

        /// <summary>How far down the camera looks, degrees (90 = straight down).</summary>
        public const float Pitch = 58f;

        /// <summary>The settings' distances (near, normal, far) as shares of <see cref="BaseHeight"/>.</summary>
        public static readonly float[] DistanceShares = { 0.8f, 1f, 1.28f };

        /// <summary>The height added at the top speed of the fastest cars, as a share.</summary>
        public const float SpeedZoom = 0.25f;

        /// <summary>Seconds of the car's velocity the view looks ahead (at most <see cref="MaxLead"/> metres).</summary>
        public const float LeadTime = 0.42f;
        public const float MaxLead = 16f;

        private const float OverviewHeight = 64f;
        private const float OverviewPitch = 56f;

        /// <summary>How far right of the middle the overview frames the leaders over the whole screen, metres.</summary>
        private const float OverviewAside = 22f;

        private Vector3 lead;
        private Vector3 leadVelocity;
        private float height;
        private float heightVelocity;
        private Vector3 overviewFocus;
        private Vector3 overviewVelocity;
        private float overviewClock;
        private float distanceShare = 1f;
        private bool snap = true;

        public Camera Camera { get; private set; }
        public CameraShake Shake { get; private set; }

        /// <summary>The car followed, or null in the overview.</summary>
        public Car Target { get; private set; }

        public Race Race { get; private set; }

        public bool IsOverview => Target == null && Race != null;

        /// <summary>The overview holds the whole track instead of drifting over the leaders.</summary>
        public bool WholeTrack { get; private set; }

        /// <summary>Where on the ground the view is centred (screen shake falls off from it).</summary>
        public Vector3 Focus { get; private set; }

        private void Awake()
        {
            Camera = GetComponent<Camera>();
            Shake = CameraShake.For(this);
            Shake.maxOffset = 0.9f;
            Shake.maxRoll = 3f;
        }

        /// <summary>Follows <paramref name="car"/> in <paramref name="viewport"/> (a normalised rect of the screen).</summary>
        public void Follow(Car car, Race race, Rect viewport)
        {
            EnsureParts();
            Target = car;
            Race = race;
            WholeTrack = false;
            Camera.rect = viewport;
            snap = true;
            Shake.Clear();
        }

        /// <summary>Looks over <paramref name="race"/> from high up: drifting over the leaders, or holding the whole track.</summary>
        public void Overview(Race race, Rect viewport, bool wholeTrack)
        {
            EnsureParts();
            Target = null;
            Race = race;
            WholeTrack = wholeTrack;
            Camera.rect = viewport;
            snap = true;
            Shake.Clear();
        }

        /// <summary>Lets go of the race: the full screen again, nothing followed.</summary>
        public void Release()
        {
            EnsureParts();
            Target = null;
            Race = null;
            Camera.rect = new Rect(0f, 0f, 1f, 1f);
            Shake.Clear();
        }

        /// <summary>The camera distance of the settings: 0 near, 1 normal, 2 far.</summary>
        public void SetDistance(int setting)
        {
            distanceShare = DistanceShares[Mathf.Clamp(setting, 0, DistanceShares.Length - 1)];
        }

        /// <summary>Jumps to where the view should be, without easing (a new race, a cut).</summary>
        public void Snap()
        {
            snap = true;
        }

        private void EnsureParts()
        {
            if (Camera == null)
            {
                Camera = GetComponent<Camera>();
            }
            if (Shake == null)
            {
                Shake = CameraShake.For(this);
            }
        }

        private void LateUpdate()
        {
            if (Race == null)
            {
                return;
            }
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f && !snap)
            {
                // Paused: the view holds still where it is (with the shake it had).
                return;
            }
            if (Target != null)
            {
                FollowTarget(deltaTime);
            }
            else if (WholeTrack)
            {
                HoldTrack();
            }
            else
            {
                DriftOverLeaders(deltaTime);
            }
            snap = false;
            if (Shake != null)
            {
                Shake.ApplyTo(transform);
            }
        }

        private void FollowTarget(float deltaTime)
        {
            Vector3 position = Target.transform.position;
            Vector3 velocity = Target.Body != null ? Target.Body.Velocity : Vector3.zero;
            velocity.y = 0f;
            Vector3 wantedLead = Vector3.ClampMagnitude(velocity * LeadTime, MaxLead);
            float speedShare = Mathf.Clamp01(velocity.magnitude / 45f);
            // A split screen view is smaller: it comes a little closer so the cars stay readable.
            float viewShare = Mathf.Lerp(0.82f, 1f, Mathf.Clamp01(Camera.rect.height * 2f - 1f));
            float wantedHeight = BaseHeight * distanceShare * viewShare * (1f + SpeedZoom * speedShare);
            if (snap || deltaTime <= 0f)
            {
                lead = wantedLead;
                leadVelocity = Vector3.zero;
                height = wantedHeight;
                heightVelocity = 0f;
            }
            else
            {
                lead = Vector3.SmoothDamp(lead, wantedLead, ref leadVelocity, 0.45f, Mathf.Infinity, deltaTime);
                height = Mathf.SmoothDamp(height, wantedHeight, ref heightVelocity, 0.9f, Mathf.Infinity, deltaTime);
            }
            Place(position + lead, height, Pitch, 0f);
        }

        private void DriftOverLeaders(float deltaTime)
        {
            overviewClock += deltaTime;
            Vector3 wanted = PackCentre(out Vector3 packVelocity);
            if (snap || deltaTime <= 0f)
            {
                overviewFocus = wanted;
                overviewVelocity = Vector3.zero;
            }
            else
            {
                // Move with the pack, then ease out what is left: no lag behind fast cars, a soft glide when the lead changes.
                overviewFocus += packVelocity * deltaTime;
                overviewFocus = Vector3.SmoothDamp(overviewFocus, wanted, ref overviewVelocity, 1.2f, Mathf.Infinity, deltaTime);
            }
            // A slow sway of the view, a crane shot rather than a security camera.
            float yaw = 14f * Mathf.Sin(overviewClock * 0.045f);
            float sway = 1f + 0.08f * Mathf.Sin(overviewClock * 0.07f + 1.3f);
            // Over the whole screen (behind the menu) the cars are framed right of centre, clear of the menu's column.
            float aside = Camera.rect.width > 0.99f ? OverviewAside : 0f;
            Place(overviewFocus, OverviewHeight * sway, OverviewPitch, yaw, aside);
        }

        /// <summary>
        /// Where the leading cars are: the leader a little ahead of itself, with the cars close behind it; and how fast that
        /// point moves (the leader's velocity).
        /// </summary>
        private Vector3 PackCentre(out Vector3 velocity)
        {
            velocity = Vector3.zero;
            List<Car> order = Race.Standings();
            Car leader = null;
            foreach (Car car in order)
            {
                if (car != null && !car.IsWrecked)
                {
                    leader = car;
                    break;
                }
            }
            if (leader == null)
            {
                return order.Count > 0 && order[0] != null ? order[0].transform.position : Race.Track != null ? Race.Track.transform.position : Vector3.zero;
            }
            velocity = leader.Body != null ? leader.Body.Velocity : Vector3.zero;
            velocity.y = 0f;
            Vector3 sum = leader.transform.position + Vector3.ClampMagnitude(velocity * 0.6f, 20f);
            float weight = 1f;
            foreach (Car car in order)
            {
                if (car == null || car == leader || car.IsWrecked)
                {
                    continue;
                }
                float apart = Vector3.Distance(car.transform.position, leader.transform.position);
                if (apart < 50f)
                {
                    float share = 0.5f * (1f - apart / 50f);
                    sum += car.transform.position * share;
                    weight += share;
                }
            }
            Vector3 centre = sum / weight;
            centre.y = 0f;
            return centre;
        }

        private void HoldTrack()
        {
            Bounds bounds = Race.Track != null ? Race.Track.Bounds : new Bounds(Vector3.zero, Vector3.one * 200f);
            if (bounds.size.sqrMagnitude < 1f && Race.Track != null && Race.Track.Path != null)
            {
                bounds = PathBounds(Race.Track);
            }
            float aspect = Mathf.Max(0.3f, Camera.aspect);
            float halfFov = Camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float needVertical = bounds.extents.z * 1.15f;
            float needHorizontal = bounds.extents.x * 1.1f / aspect;
            float distance = Mathf.Max(needVertical, needHorizontal) / Mathf.Tan(halfFov);
            Place(new Vector3(bounds.center.x, 0f, bounds.center.z), distance * Mathf.Sin(80f * Mathf.Deg2Rad), 80f, 0f);
        }

        private static Bounds PathBounds(Track track)
        {
            var bounds = new Bounds(RacePathPoint(track, 0), Vector3.zero);
            foreach (Vector2 point in track.Path.Samples)
            {
                bounds.Encapsulate(new Vector3(point.x, 0f, point.y));
            }
            bounds.Expand(track.HalfWidth * 2f);
            return bounds;
        }

        private static Vector3 RacePathPoint(Track track, float distance)
        {
            Vector2 point = track.Path.PointAt(distance);
            return new Vector3(point.x, 0f, point.y);
        }

        /// <summary>
        /// Looks at <paramref name="focus"/> from <paramref name="altitude"/> up at <paramref name="pitch"/>, turned
        /// <paramref name="yaw"/>, with the focus <paramref name="aside"/> metres right of the middle of the view.
        /// </summary>
        private void Place(Vector3 focus, float altitude, float pitch, float yaw, float aside = 0f)
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            float distance = altitude / Mathf.Sin(pitch * Mathf.Deg2Rad);
            transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance - rotation * Vector3.right * aside, rotation);
            Focus = focus;
        }
    }
}
