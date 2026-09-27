using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The beams and zaps of the ship's strike weapons (the deathray, the twin laser, the laser turret): line renderers
    /// on the ship prefab, drawn from a gun to what they hit. The stand-ins of other pilots show their beams from the
    /// pose's beam bit (<see cref="ShowPose"/>).
    ///
    /// Expected on the ship prefab (content): a child "PlayerBeam" with this component, and under it "Beam0" and "Beam1"
    /// (LineRenderer, 2 positions, world space, an additive glow material; the deathray uses Beam0, the twin laser both),
    /// "Zap" (LineRenderer, 2 positions or more, world space, thin and bright) and optional glow sprites "Tip0" / "Tip1"
    /// (the hot spot where a beam ends) and "Flare" (a quad at the gun while a beam is on). Anything missing is skipped.
    /// A beam or zap that is not refreshed for a few frames hides itself, so nothing stays on when the ship stops being
    /// stepped.
    /// </summary>
    public class PlayerBeam : MonoBehaviour
    {
        [Tooltip("One line per beam (the deathray uses the first, the twin laser both).")]
        [SerializeField] internal LineRenderer[] beams = new LineRenderer[0];
        [Tooltip("The laser turret's zap.")]
        [SerializeField] internal LineRenderer zap;
        [Tooltip("Seconds a zap shows.")]
        [SerializeField] internal float zapTime = 0.08f;
        [Tooltip("Optional glow at the end of each beam (where it hits).")]
        [SerializeField] internal Transform[] tips = new Transform[0];
        [Tooltip("Optional glow at the gun while a beam is on.")]
        [SerializeField] internal Transform flare;
        [Tooltip("Width of a beam line in meters (it flickers around it).")]
        [SerializeField] internal float beamWidth = 0.55f;
        [Tooltip("Width of the zap line in meters.")]
        [SerializeField] internal float zapWidth = 0.18f;
        [Tooltip("Depth of the lines (negative is toward the camera, over the air units).")]
        [SerializeField] internal float depth = -0.1f;

        /// <summary>Frames a beam stays on without being refreshed.</summary>
        private const int KeepFrames = 2;

        private readonly int[] beamFrames = new int[2];
        private float zapLeft;
        private float poseClock;
        private int humFrame;
        private AsteroidsAudio humming;


        /// <summary>
        /// Where beam <paramref name="index"/> of <paramref name="item"/> starts for a ship at <paramref name="shipPosition"/>
        /// with its gun at <paramref name="gunPoint"/>: the deathray from the nose, the twin laser from the wing tips.
        /// </summary>
        public static Vector2 BeamOrigin(StrikeItem item, int index, Vector2 shipPosition, Vector2 gunPoint)
        {
            StrikeWeaponInfo info = StrikeWeaponRules.Info(item);
            if (info.Barrels <= 1)
            {
                return shipPosition + new Vector2(0f, gunPoint.y);
            }
            float side = (index - (info.Barrels - 1) * 0.5f) * info.BarrelSpacing;
            return shipPosition + new Vector2(side, gunPoint.y * 0.4f);
        }


        /// <summary>Shows beam <paramref name="index"/> from <paramref name="from"/> to <paramref name="to"/>, or hides it.</summary>
        public void ShowBeam(int index, Vector2 from, Vector2 to, bool on)
        {
            if (index < 0 || index >= beamFrames.Length)
            {
                return;
            }
            LineRenderer line = index < beams.Length ? beams[index] : null;
            Transform tip = index < tips.Length ? tips[index] : null;
            beamFrames[index] = on ? Time.frameCount : -KeepFrames - 1;
            if (line != null)
            {
                line.enabled = on;
                if (on)
                {
                    line.useWorldSpace = true;
                    line.positionCount = 2;
                    line.SetPosition(0, new Vector3(from.x, from.y, depth));
                    line.SetPosition(1, new Vector3(to.x, to.y, depth));
                    float flicker = 0.8f + 0.35f * Mathf.PerlinNoise(Time.time * 40f, index * 3.7f);
                    line.widthMultiplier = beamWidth * flicker;
                }
            }
            if (tip != null)
            {
                tip.gameObject.SetActive(on);
                if (on)
                {
                    tip.position = new Vector3(to.x, to.y, depth - 0.02f);
                    tip.localScale = Vector3.one * (0.8f + 0.4f * Mathf.PerlinNoise(Time.time * 25f, 1.3f + index));
                }
            }
            UpdateFlare(from);
        }


        /// <summary>A laser turret zap from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public void ShowZap(Vector2 from, Vector2 to)
        {
            zapLeft = zapTime;
            if (zap == null)
            {
                return;
            }
            zap.useWorldSpace = true;
            // A slightly jagged bolt: the middle points wander off the straight line.
            int points = Mathf.Max(2, zap.positionCount);
            zap.positionCount = points;
            Vector2 along = to - from;
            var side = new Vector2(-along.y, along.x).normalized;
            for (int i = 0; i < points; i++)
            {
                float t = points > 1 ? i / (float)(points - 1) : 0f;
                float jag = i == 0 || i == points - 1 ? 0f : (Mathf.PerlinNoise(Time.time * 60f, i * 1.7f) - 0.5f) * 0.6f;
                Vector2 point = from + along * t + side * jag;
                zap.SetPosition(i, new Vector3(point.x, point.y, depth));
            }
            zap.widthMultiplier = zapWidth;
            zap.enabled = true;
        }


        /// <summary>Hides every beam and zap.</summary>
        public void HideAll()
        {
            for (int i = 0; i < beamFrames.Length; i++)
            {
                beamFrames[i] = -KeepFrames - 1;
            }
            foreach (LineRenderer line in beams)
            {
                if (line != null)
                {
                    line.enabled = false;
                }
            }
            foreach (Transform tip in tips)
            {
                if (tip != null)
                {
                    tip.gameObject.SetActive(false);
                }
            }
            if (flare != null)
            {
                flare.gameObject.SetActive(false);
            }
            zapLeft = 0f;
            if (zap != null)
            {
                zap.enabled = false;
            }
            Hum(humming, false);
        }


        /// <summary>The beam's hum on <paramref name="sounds"/> (the local ship only): on while a beam weapon fires.</summary>
        public void Hum(AsteroidsAudio sounds, bool on)
        {
            if (on && sounds != null)
            {
                humming = sounds;
                humFrame = Time.frameCount;
                sounds.SetBeam(true);
            }
            else if (!on && humming != null)
            {
                humming.SetBeam(false);
                humming = null;
            }
        }


        /// <summary>
        /// A stand-in of another pilot: shows the beams its pose reports (<paramref name="beamKind"/>, see
        /// <see cref="StrikeGunnery.BeamKindOf"/>; 0 none) with the on/off pulse drawn here, each to the first enemy in its
        /// column on this client's field. Deals no damage. Call it every frame.
        /// </summary>
        public void ShowPose(AsteroidsPlayer ship, int beamKind, float deltaTime)
        {
            StrikeItem item = StrikeGunnery.BeamOfKind(beamKind);
            if (ship == null || item == StrikeItem.MachineGun || !ship.IsAlive)
            {
                poseClock = 0f;
                ShowBeam(0, Vector2.zero, Vector2.zero, false);
                ShowBeam(1, Vector2.zero, Vector2.zero, false);
                return;
            }
            poseClock += deltaTime;
            float fireRate = ship.PlayerSettings != null ? ship.PlayerSettings.FireRateMultiplier : 1f;
            bool on = StrikeGunnery.IsBeamOn(item, poseClock, fireRate);
            StrikeWeaponInfo info = StrikeWeaponRules.Info(item);
            Vector2 gun = ship.PlayerSettings != null ? ship.PlayerSettings.GunPoint : new Vector2(0f, 0.8f);
            SpaceField field = ship.Field;
            float top = field != null && field.Playground != null ? field.Playground.Top + 1f : ship.Position.y + 25f;
            for (int b = 0; b < 2; b++)
            {
                if (b >= info.Barrels)
                {
                    ShowBeam(b, Vector2.zero, Vector2.zero, false);
                    continue;
                }
                Vector2 from = BeamOrigin(item, b, ship.Position, gun);
                Shootable target = field != null ? field.FirstInColumn(from.x, info.BeamHalfWidth, from.y, Altitude.Both) : null;
                Vector2 to = target != null ? new Vector2(from.x, Mathf.Max(from.y, target.Position.y - target.Radius * 0.5f)) : new Vector2(from.x, top);
                ShowBeam(b, from, to, on);
            }
        }


        private void UpdateFlare(Vector2 from)
        {
            if (flare == null)
            {
                return;
            }
            bool any = IsShowing(0) || IsShowing(1);
            flare.gameObject.SetActive(any);
            if (any)
            {
                flare.position = new Vector3(from.x, from.y, depth - 0.03f);
            }
        }


        private bool IsShowing(int index)
        {
            return Time.frameCount - beamFrames[index] <= KeepFrames;
        }


        private void LateUpdate()
        {
            // Beams not refreshed lately go out (the ship stopped being stepped: the mission ended or it was destroyed).
            bool any = false;
            for (int i = 0; i < beamFrames.Length; i++)
            {
                bool showing = IsShowing(i);
                any |= showing;
                if (showing)
                {
                    continue;
                }
                if (i < beams.Length && beams[i] != null && beams[i].enabled)
                {
                    beams[i].enabled = false;
                }
                if (i < tips.Length && tips[i] != null && tips[i].gameObject.activeSelf)
                {
                    tips[i].gameObject.SetActive(false);
                }
            }
            if (!any)
            {
                if (flare != null && flare.gameObject.activeSelf)
                {
                    flare.gameObject.SetActive(false);
                }
            }
            if (humming != null && Time.frameCount - humFrame > KeepFrames)
            {
                Hum(humming, false);
            }
            if (zapLeft > 0f)
            {
                zapLeft -= Time.deltaTime;
                if (zapLeft <= 0f && zap != null)
                {
                    zap.enabled = false;
                }
            }
        }


        private void OnDisable()
        {
            HideAll();
        }
    }
}
