using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// A mortar shell lobbed from a car to a point ahead: it climbs on an arc over the cars (<see cref="CombatRules.MortarArc"/>)
    /// and comes down where it was aimed after <see cref="CombatRules.MortarFlightSeconds"/>, trailing smoke and
    /// whistling as it falls, while a red ring on the road warns where it will land. It bursts like a big rocket:
    /// <see cref="CombatRules.MortarDamage"/> at the centre of a <see cref="CombatRules.MortarBlastRadius"/> blast that throws
    /// the cars round it (<see cref="Blast"/>: blast shielding helps), a fireball, a shake and a scorch mark. It flies over
    /// everything (no barrier stops it); the kill is the car that fired it.
    ///
    /// Every device shows the shell; the blast is judged on each car by its own device (<see cref="CarHealth.Authoritative"/>),
    /// so a shell shown for the room online (<see cref="Authoritative"/> false) hurts only the cars driven here.
    /// <see cref="Launch"/> plays the launch's thump and the whistle.
    /// </summary>
    public class MortarShell : MonoBehaviour
    {
        private static readonly List<MortarShell> active = new List<MortarShell>();
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private Race race;
        private float apex;
        private float age;
        private bool landed;
        private TrailRenderer trail;
        private Renderer warning;
        private MaterialPropertyBlock block;

        /// <summary>The shells in the air now (development checks).</summary>
        public static IReadOnlyList<MortarShell> Active => active;

        public Car Owner { get; private set; }

        /// <summary>Where the shell left from and where it lands (on the ground).</summary>
        public Vector3 From { get; private set; }

        public Vector3 Target { get; private set; }

        /// <summary>How far through its flight the shell is, 0 to 1.</summary>
        public float Progress => Mathf.Clamp01(age / CombatRules.MortarFlightSeconds);

        public Vector3 Position => transform.position;

        /// <summary>Online: every device shows the shell; each judges the blast only on the cars it drives.</summary>
        public bool Authoritative { get; set; } = true;

        /// <summary>A shell landed and burst (development checks).</summary>
        public static event System.Action<MortarShell> Burst;

        /// <summary>Lobs a shell of <paramref name="owner"/> from <paramref name="from"/> to land at <paramref name="target"/>.</summary>
        public static MortarShell Launch(Race race, DeathRaceContent content, Vector3 from, Vector3 target, Car owner, bool authoritative = true)
        {
            GameObject go;
            if (content != null && content.mortarShell != null)
            {
                go = Instantiate(content.mortarShell);
                go.name = "Mortar Shell";
            }
            else
            {
                go = Fallback(race != null && race.Effects != null ? race.Effects.Theme : null);
            }
            go.transform.SetParent(race != null ? race.Root : null, false);
            foreach (Collider collider in go.GetComponentsInChildren<Collider>(true))
            {
                DestroyImmediate(collider);
            }
            go.transform.position = from;
            MortarShell shell = go.AddComponent<MortarShell>();
            shell.race = race;
            shell.Owner = owner;
            shell.From = from;
            shell.Target = new Vector3(target.x, 0f, target.z);
            shell.Authoritative = authoritative;
            shell.apex = CombatRules.MortarApex(new Vector2(target.x - from.x, target.z - from.z).magnitude);
            shell.Dress();
            shell.Pose();
            active.Add(shell);
            RaceAudio audio = race != null && race.Audio != null ? race.Audio : RaceAudio.Current;
            audio?.MortarLaunch(from);
            audio?.MortarWhistle(shell.Target);
            race?.Effects?.Muzzle(go.transform);
            return shell;
        }

        private void Dress()
        {
            var smoke = new GameObject("Smoke Trail");
            smoke.transform.SetParent(transform, false);
            smoke.transform.localPosition = new Vector3(0f, 0f, -0.4f);
            trail = smoke.AddComponent<TrailRenderer>();
            trail.time = 0.8f;
            trail.minVertexDistance = 0.5f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.3f, 1.1f), new Keyframe(1f, 2.2f));
            trail.textureMode = LineTextureMode.Tile;
            trail.textureScale = new Vector2(0.6f, 1f);
            trail.numCapVertices = 2;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.sharedMaterial = CombatArt.SmokeTrail;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.85f, 0.82f, 0.78f), 0f), new GradientColorKey(new Color(0.55f, 0.53f, 0.5f), 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.55f, 0.4f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;

            // Where it will come down: a red ring the size of the blast, pulsing faster as the shell falls.
            var ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ring.name = "Landing Ring";
            DestroyImmediate(ring.GetComponent<Collider>());
            ring.transform.SetParent(transform.parent, false);
            ring.transform.SetPositionAndRotation(new Vector3(Target.x, RaceEffects.GroundMarkHeight + 0.05f, Target.z), Quaternion.Euler(90f, 0f, 0f));
            float size = CombatRules.MortarBlastRadius * 2f / 0.82f;
            ring.transform.localScale = new Vector3(size, size, 1f);
            warning = ring.GetComponent<MeshRenderer>();
            warning.sharedMaterial = CombatArt.Ring;
            warning.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            warning.receiveShadows = false;
        }

        private void FixedUpdate()
        {
            Step(Time.fixedDeltaTime);
        }

        /// <summary>One step of the flight; at its end the shell bursts (public for the tests, which step it by hand).</summary>
        public void Step(float deltaTime)
        {
            if (landed || deltaTime <= 0f)
            {
                return;
            }
            age += deltaTime;
            Pose();
            if (age >= CombatRules.MortarFlightSeconds)
            {
                Land();
            }
        }

        /// <summary>The shell where it is on its arc, pointing along its flight; the warning ring pulses.</summary>
        private void Pose()
        {
            float t = Progress;
            Vector3 here = CombatRules.MortarArc(From, Target, apex, t);
            Vector3 ahead = CombatRules.MortarArc(From, Target, apex, Mathf.Min(1f, t + 0.02f)) - here;
            transform.position = here;
            if (ahead.sqrMagnitude > 1e-6f)
            {
                transform.rotation = Quaternion.LookRotation(ahead.normalized, Vector3.up);
            }
            if (warning != null)
            {
                block ??= new MaterialPropertyBlock();
                float pulse = 0.5f + 0.5f * Mathf.Sin(age * Mathf.Lerp(10f, 32f, t));
                Color tint = new Color(1f, 0.12f, 0.08f, 1f) * 2.2f;
                tint.a = Mathf.Lerp(0.25f, 0.9f, t) * (0.55f + 0.45f * pulse);
                warning.GetPropertyBlock(block);
                block.SetColor(ColorId, tint);
                warning.SetPropertyBlock(block);
            }
        }

        /// <summary>Down: the blast where it was aimed.</summary>
        private void Land()
        {
            landed = true;
            Vector3 at = Target;
            RaceEffects effects = race != null ? race.Effects : null;
            effects?.Explosion(at + Vector3.up * 0.5f, 1.45f);
            effects?.Scorch(at, 1.35f);
            effects?.Shake(at, 0.7f);
            (race != null && race.Audio != null ? race.Audio : RaceAudio.Current)?.Explosion(at, false);
            Burst?.Invoke(this);
            // Every device bursts its copy; each car's own device judges what the blast does to it (CarHealth.Authoritative).
            Blast.Explode(race, at, CombatRules.MortarBlastRadius, CombatRules.MortarDamage, CombatRules.MortarPush, DamageSource.Mortar, Owner);
            Leave();
        }

        /// <summary>The smoke stays and fades where it was; the shell and its ring are gone.</summary>
        private void Leave()
        {
            if (trail != null)
            {
                trail.transform.SetParent(transform.parent, true);
                trail.emitting = false;
                CombatArt.Discard(trail.gameObject, trail.time + 0.2f);
                trail = null;
            }
            if (warning != null)
            {
                CombatArt.Discard(warning.gameObject);
                warning = null;
            }
            active.Remove(this);
            CombatArt.Discard(gameObject);
        }

        private void OnDestroy()
        {
            active.Remove(this);
            if (warning != null)
            {
                CombatArt.Discard(warning.gameObject);
            }
        }

        /// <summary>A stand-in until the builders made the shell: an olive body with a dark nose and a brass band.</summary>
        private static GameObject Fallback(DeathRaceTheme theme)
        {
            var root = new GameObject("Mortar Shell");
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f);
            CombatArt.Paint(body, new Color(0.36f, 0.39f, 0.21f, 1f), false, theme);
            var band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            band.name = "Band";
            band.transform.SetParent(root.transform, false);
            band.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            band.transform.localPosition = new Vector3(0f, 0f, 0.12f);
            band.transform.localScale = new Vector3(0.4f, 0.05f, 0.4f);
            CombatArt.Paint(band, new Color(0.83f, 0.66f, 0.26f, 1f), false, theme);
            return root;
        }
    }
}
