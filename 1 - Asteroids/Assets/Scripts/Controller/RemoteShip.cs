using Gamebox;
using Gamebox.Online;
using TMPro;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The stand-in of a ship that a pilot flies on another device in a shared mission. It is an instance of the ship
    /// prefab that is never simulated and takes no damage here: it is moved between the poses that arrive from its owner
    /// (<see cref="PoseInterpolator"/>, across the wrapping edges of the playfield), shows what they report (thrust, dash,
    /// shield, a failing hull, the hull picked in the hangar) and carries the pilot's name in the colour of their seat.
    /// Enemies, mines and pickups treat it like any other ship (see <see cref="SpaceField.NearestShip"/>).
    /// </summary>
    [RequireComponent(typeof(AsteroidsPlayer))]
    public class RemoteShip : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly PoseInterpolator poses = new PoseInterpolator();
        private AsteroidsPlayer player;
        private SpaceField field;
        private PlayerSettings[] hangar;
        private TextMeshPro label;
        private Color tint = Color.white;
        private float hullStrength = 100f;
        private ShipPose shown;
        private int shownHull = -1;
        private float lastHeading;
        private float turn;
        private bool placed;

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
            transform.position = new Vector3(start.x, start.y, 0f);
            transform.rotation = Quaternion.identity;
            ShowHull(0);
            Tint();
            BuildLabel(pilotName);
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
            if (poses.Sample(Time.unscaledTime, out Vector3 position, out _, out float heading))
            {
                transform.position = new Vector3(position.x, position.y, 0f);
                transform.rotation = Quaternion.Euler(0f, 0f, heading);
                if (placed && deltaTime > 0.0001f)
                {
                    // The roll into a turn comes from how fast the heading changes (250 degrees a second is a full turn).
                    float rate = Mathf.DeltaAngle(lastHeading, heading) / deltaTime;
                    turn = Mathf.Lerp(turn, Mathf.Clamp(rate / 250f, -1f, 1f), 1f - Mathf.Exp(-10f * deltaTime));
                }
                lastHeading = heading;
                placed = true;
            }
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
            if (label != null)
            {
                // The name stays upright above the ship however it turns.
                label.transform.position = transform.position + new Vector3(0f, 1.45f, -0.3f);
                label.transform.rotation = Quaternion.identity;
            }
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


        /// <summary>The halo under the ship glows in the colour of the seat.</summary>
        private void Tint()
        {
            Transform halo = transform.Find("Halo");
            if (halo == null || !halo.TryGetComponent(out Renderer glow))
            {
                return;
            }
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, new Color(tint.r * 1.6f, tint.g * 1.6f, tint.b * 1.6f, 0.5f));
            glow.SetPropertyBlock(block);
        }


        private void BuildLabel(string pilotName)
        {
            if (label == null)
            {
                var labelObject = new GameObject("Name");
                labelObject.transform.SetParent(transform, false);
                label = labelObject.AddComponent<TextMeshPro>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 3.2f;
                label.fontStyle = FontStyles.Bold;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
                label.rectTransform.sizeDelta = new Vector2(8f, 1f);
                label.sortingOrder = 5;
            }
            label.text = pilotName;
            label.color = tint;
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
