using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The stand-in of a ship that a pilot flies on another device in a shared mission. It is an instance of the ship
    /// prefab that is never simulated and takes no damage here: it is moved between the poses that arrive from its owner
    /// (<see cref="PoseInterpolator"/>, across the wrapping edges of the playfield), shows what they report (thrust, dash,
    /// shield, a failing hull, the hull picked in the hangar) and carries the pilot's name in the colour of their seat
    /// (<see cref="PilotTag"/>).
    /// Enemies, mines and pickups treat it like any other ship (see <see cref="SpaceField.NearestShip"/>). In a strike
    /// mission the playfield does not wrap, the ship banks with its sideways speed, and a held beam weapon (the pose's
    /// beam bit) is drawn here with its own on and off pulse, up to the first enemy in its column: only a picture, the
    /// beam's damage is dealt on the pilot's own device.
    /// </summary>
    [RequireComponent(typeof(AsteroidsPlayer))]
    public class RemoteShip : MonoBehaviour
    {
        /// <summary>Sideways speed (m/s) at which a strike stand-in banks all the way.</summary>
        private const float BankSpeed = 17f;

        /// <summary>How far ahead of the ship's middle a beam starts.</summary>
        private const float BeamNose = 0.8f;

        private readonly PoseInterpolator poses = new PoseInterpolator();
        private AsteroidsPlayer player;
        private SpaceField field;
        private PlayerSettings[] hangar;
        private Color tint = Color.white;
        private float hullStrength = 100f;
        private ShipPose shown;
        private int shownHull = -1;
        private float lastHeading;
        private float turn;
        private bool placed;
        private PlayerBeam beams;
        private bool beamsShown;

        public AsteroidsPlayer Player => player;

        public int Seat => player != null ? player.Seat : -1;

        /// <summary>Whether the pilot still flies, as far as their last pose says.</summary>
        public bool Flying => shown.Alive;


        /// <summary>
        /// Sets the stand-in up for the pilot at <paramref name="seat"/>. <paramref name="hulls"/> are the ships of the
        /// hangar (the pose says which one the pilot flies), <paramref name="hullPoints"/> the hull strength of the mission.
        /// </summary>
        public void Setup(SpaceField playfield, int seat, string pilotName, PlayerSettings[] hulls, float hullPoints, Vector2 start)
        {
            player = GetComponent<AsteroidsPlayer>();
            field = playfield;
            hangar = hulls;
            hullStrength = hullPoints;
            tint = CoopRules.SeatColor(seat);
            player.Assign(seat, PlayerControl.Remote, pilotName);
            name = $"Ship of {pilotName}";
            var autopilot = GetComponent<AsteroidsAutopilot>();
            if (autopilot != null)
            {
                autopilot.enabled = false;
            }
            var strikePilot = GetComponentInChildren<StrikeAutopilot>(true);
            if (strikePilot != null)
            {
                strikePilot.enabled = false;
            }
            beams = GetComponentInChildren<PlayerBeam>(true);
            beamsShown = false;
            transform.position = new Vector3(start.x, start.y, 0f);
            transform.rotation = Quaternion.identity;
            ShowHull(0);
            PilotTag.Show(gameObject, pilotName, tint);
            shown = new ShipPose { Alive = true, Health = 1f, Shield = 0.5f, Invulnerable = true };
            player.Mirror(shown, hullStrength);
            player.visuals?.ResetVisuals();
        }


        /// <summary>A pose of the pilot arrived.</summary>
        public void Pose(RoomPoseInfo pose)
        {
            if (field != null && field.Playground != null)
            {
                Vector2 period = field.Playground.WrapPeriod(player != null ? player.Radius : 0.6f);
                poses.WrapSize = new Vector3(period.x, period.y, 0f);
            }
            poses.Add(pose, Time.unscaledTime);
            ShipPose next = ShipPose.Unpack(pose.State, pose.Value);
            bool wasAlive = shown.Alive;
            bool wasDashing = shown.Dashing;
            float shield = shown.Shield;
            shown = next;
            ShowHull(next.Hull);
            player.Mirror(next, hullStrength * (player.PlayerSettings != null ? player.PlayerSettings.HullMultiplier : 1f));
            Vector2 at = pose.Position;
            if (wasAlive && !next.Alive)
            {
                ShowBeams(false);
                field?.Effects?.ShipExplosion(transform.position, tint);
                field?.Sounds?.ShipExplode();
                field?.CameraRig?.Shake(0.4f);
            }
            else if (!wasAlive && next.Alive)
            {
                placed = false;
                field?.Effects?.WarpIn(at, 1.8f, tint);
                player.visuals?.ResetVisuals();
            }
            if (next.Dashing && !wasDashing && next.Alive)
            {
                float radians = (pose.Heading + 90f) * Mathf.Deg2Rad;
                field?.Effects?.DashTrail(at, new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)), tint);
            }
            if (next.Alive && next.Shield < shield - 0.02f)
            {
                player.visuals?.ShieldHit(Vector2.up);
            }
            SetVisible(next.Alive);
        }


        private void Update()
        {
            if (player == null)
            {
                return;
            }
            float deltaTime = Time.deltaTime;
            bool strike = IsStrikeField;
            if (poses.Sample(Time.unscaledTime, out Vector3 position, out Vector3 velocity, out float heading))
            {
                transform.position = new Vector3(position.x, position.y, 0f);
                transform.rotation = Quaternion.Euler(0f, 0f, heading);
                if (strike && deltaTime > 0.0001f)
                {
                    // A strike ship keeps its nose up and rolls with its sideways speed (to the left, a positive turn).
                    turn = Mathf.Lerp(turn, Mathf.Clamp(-velocity.x / BankSpeed, -1f, 1f), 1f - Mathf.Exp(-10f * deltaTime));
                }
                else if (placed && deltaTime > 0.0001f)
                {
                    // The roll into a turn comes from how fast the heading changes (250 degrees a second is a full turn).
                    float rate = Mathf.DeltaAngle(lastHeading, heading) / deltaTime;
                    turn = Mathf.Lerp(turn, Mathf.Clamp(rate / 250f, -1f, 1f), 1f - Mathf.Exp(-10f * deltaTime));
                }
                lastHeading = heading;
                placed = true;
            }
            ShowBeams(strike && shown.Alive && shown.Beam);
            if (!shown.Alive)
            {
                return;
            }
            if (player.visuals != null)
            {
                player.visuals.Animate(new ShipVisuals.Look
                {
                    Thrust = shown.Thrusting ? 1f : 0f,
                    Turn = turn,
                    Dashing = shown.Dashing,
                    Blinking = shown.Invulnerable && !shown.Dashing,
                    Shield = shown.Shield,
                    Hull = shown.Health
                }, deltaTime);
            }
            player.TickDrones(deltaTime);
        }


        /// <summary>The playfield is a strike mission's (it does not wrap).</summary>
        private bool IsStrikeField => field != null && field.Playground != null && !field.Playground.Wraps;


        /// <summary>
        /// The beams of the weapon the pilot holds (the pose's beam kind: 1 the deathray from the nose, 2 the twin laser
        /// from the wing tips), on and off with the weapon's own pulse, each up to the first enemy in its column or the
        /// top of the playfield; hidden when <paramref name="held"/> is false.
        /// </summary>
        private void ShowBeams(bool held)
        {
            if (beams == null)
            {
                return;
            }
            if (!held)
            {
                if (beamsShown)
                {
                    beamsShown = false;
                    beams.HideAll();
                }
                return;
            }
            StrikeItem item = shown.BeamKind == 2 ? StrikeItem.TwinLaser : StrikeItem.Deathray;
            StrikeWeaponInfo info = StrikeWeaponRules.Info(item);
            float period = Mathf.Max(0.05f, info.Rate);
            bool on = Mathf.Repeat(Time.time, period) < Mathf.Max(0.01f, info.BeamOn);
            int count = Mathf.Max(1, info.Barrels);
            Vector2 at = transform.position;
            float top = field.Playground.Top;
            for (int i = 0; i < count; i++)
            {
                float offset = count > 1 ? (i - (count - 1) * 0.5f) * info.BarrelSpacing : 0f;
                var from = new Vector2(at.x + offset, at.y + BeamNose);
                Shootable target = field.FirstInColumn(from.x, info.BeamHalfWidth, from.y, info.Mask);
                var to = new Vector2(from.x, target != null ? Mathf.Max(from.y, target.Position.y - target.Radius * 0.5f) : top);
                beams.ShowBeam(i, from, to, on);
            }
            beamsShown = true;
        }


        private void ShowHull(int index)
        {
            if (index == shownHull || hangar == null || hangar.Length == 0)
            {
                return;
            }
            shownHull = index;
            PlayerSettings hull = hangar[Mathf.Clamp(index, 0, hangar.Length - 1)];
            if (hull != null)
            {
                player.ApplyHull(hull);
            }
        }


        private void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }
    }
}
